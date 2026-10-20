using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using IdeaGenerator.Core.Models;
using Microsoft.Data.Sqlite;

namespace IdeaGenerator.Core.Services;

/// <summary>
/// Local persistence for the Idea Library. Accesses SQLite directly
/// (no repository abstraction). Owns schema versioning, additive migrations,
/// and idempotent sanitized sample seeding.
/// </summary>
public sealed class IdeaDataService : IDisposable
{
    private const string SampleSeedKey = "SampleIdeaIds";

    private readonly SqliteConnection _connection;
    private readonly List<Action<SqliteConnection>> _migrations = new();
    private int _schemaVersion;

    public IdeaDataService(string databasePath)
    {
        _connection = new SqliteConnection($"Data Source={databasePath}");
        _connection.Open();

        RegisterMigrations();
        Migrate();
    }

    public int SchemaVersion => _schemaVersion;

    // ─────────────────────────── schema ───────────────────────────

    private void RegisterMigrations()
    {
        // Schema version 1: initial schema.
        _migrations.Add(conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                CREATE TABLE ideas (
                    Id                   TEXT PRIMARY KEY,
                    Title                TEXT NOT NULL,
                    ContentType          INTEGER NOT NULL,
                    UseCaseSubtype       INTEGER,
                    Category             TEXT NOT NULL,
                    Status               INTEGER NOT NULL,
                    IsPinned             INTEGER NOT NULL DEFAULT 0,
                    Source               INTEGER NOT NULL,
                    TargetCustomer       TEXT NOT NULL DEFAULT '',
                    CustomerProblem      TEXT NOT NULL DEFAULT '',
                    ShortDescription     TEXT NOT NULL DEFAULT '',
                    PrimaryControl       TEXT NOT NULL DEFAULT '',
                    SupportingControls   TEXT NOT NULL DEFAULT '',
                    ControlFeatures      TEXT NOT NULL DEFAULT '',
                    Keywords             TEXT NOT NULL DEFAULT '',
                    WorkflowSummary      TEXT NOT NULL DEFAULT '',
                    AiOperation          TEXT NOT NULL DEFAULT '',
                    ScreenNames          TEXT NOT NULL DEFAULT '',
                    OriginalUserInput    TEXT,
                    DetailsJson          TEXT,
                    CreatedAt            TEXT NOT NULL,
                    UpdatedAt            TEXT NOT NULL
                );

                CREATE INDEX idx_ideas_content_type ON ideas (ContentType);
                CREATE INDEX idx_ideas_category     ON ideas (Category);
                CREATE INDEX idx_ideas_status       ON ideas (Status);
                CREATE INDEX idx_ideas_primary_ctl  ON ideas (PrimaryControl);
                """;
            cmd.ExecuteNonQuery();
        });
    }

    private void Migrate()
    {
        using var check = _connection.CreateCommand();
        check.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='ideas'";
        var hasIdeasTable = check.ExecuteScalar() is not null;

        _schemaVersion = 0;
        if (hasIdeasTable)
        {
            using var v = _connection.CreateCommand();
            v.CommandText = "SELECT value FROM meta WHERE key = 'SchemaVersion'";
            var stored = v.ExecuteScalar() as string;
            if (int.TryParse(stored, out var parsed))
            {
                _schemaVersion = parsed;
            }
        }

        if (_schemaVersion >= _migrations.Count)
        {
            return; // Already up to date.
        }

        // Additive migrations: run every registered step newer than the stored
        // schema version, in order. Existing user data is never dropped or
        // rewritten by design.
        var tx = _connection.BeginTransaction();
        try
        {
            for (var i = _schemaVersion; i < _migrations.Count; i++)
            {
                _migrations[i](_connection);
                _schemaVersion = i + 1;
                SetMeta("SchemaVersion", _schemaVersion.ToString());
            }

            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    /// <summary>
    /// Future additive migrations: append a step to <see cref="_migrations"/> and
    /// it will run once for databases with a lower <see cref="SchemaVersion"/>.
    /// Existing user data is never dropped or rewritten by design.
    /// </summary>
    private void SetMeta(string key, string value)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS meta (
                key   TEXT PRIMARY KEY,
                value TEXT NOT NULL
            );
            INSERT INTO meta (key, value) VALUES (@key, @value)
            ON CONFLICT(key) DO UPDATE SET value = excluded.value;
            """;
        cmd.Parameters.AddWithValue("@key", key);
        cmd.Parameters.AddWithValue("@value", value);
        cmd.ExecuteNonQuery();
    }

    private string? GetMeta(string key)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT value FROM meta WHERE key = @key";
        cmd.Parameters.AddWithValue("@key", key);
        return cmd.ExecuteScalar() as string;
    }

    // ─────────────────────────── CRUD ───────────────────────────

    public int Save(Idea idea)
    {
        idea.UpdatedAt = DateTime.UtcNow;

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO ideas (
                Id, Title, ContentType, UseCaseSubtype, Category, Status, IsPinned, Source,
                TargetCustomer, CustomerProblem, ShortDescription, PrimaryControl,
                SupportingControls, ControlFeatures, Keywords, WorkflowSummary, AiOperation,
                ScreenNames, OriginalUserInput, DetailsJson, CreatedAt, UpdatedAt
            ) VALUES (
                @Id, @Title, @ContentType, @UseCaseSubtype, @Category, @Status, @IsPinned, @Source,
                @TargetCustomer, @CustomerProblem, @ShortDescription, @PrimaryControl,
                @SupportingControls, @ControlFeatures, @Keywords, @WorkflowSummary, @AiOperation,
                @ScreenNames, @OriginalUserInput, @DetailsJson, @CreatedAt, @UpdatedAt
            )
            ON CONFLICT(Id) DO UPDATE SET
                Title = excluded.Title,
                ContentType = excluded.ContentType,
                UseCaseSubtype = excluded.UseCaseSubtype,
                Category = excluded.Category,
                Status = excluded.Status,
                IsPinned = excluded.IsPinned,
                Source = excluded.Source,
                TargetCustomer = excluded.TargetCustomer,
                CustomerProblem = excluded.CustomerProblem,
                ShortDescription = excluded.ShortDescription,
                PrimaryControl = excluded.PrimaryControl,
                SupportingControls = excluded.SupportingControls,
                ControlFeatures = excluded.ControlFeatures,
                Keywords = excluded.Keywords,
                WorkflowSummary = excluded.WorkflowSummary,
                AiOperation = excluded.AiOperation,
                ScreenNames = excluded.ScreenNames,
                OriginalUserInput = excluded.OriginalUserInput,
                DetailsJson = excluded.DetailsJson,
                UpdatedAt = excluded.UpdatedAt;
            """;
        BindIdea(cmd, idea);
        return cmd.ExecuteNonQuery();
    }

    public bool Delete(Guid id)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "DELETE FROM ideas WHERE Id = @Id";
        cmd.Parameters.AddWithValue("@Id", id.ToString());
        return cmd.ExecuteNonQuery() > 0;
    }

    public bool SetPinned(Guid id, bool pinned)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "UPDATE ideas SET IsPinned = @Pinned, UpdatedAt = @Now WHERE Id = @Id";
        cmd.Parameters.AddWithValue("@Pinned", pinned ? 1 : 0);
        cmd.Parameters.AddWithValue("@Now", DateTime.UtcNow.ToString("O"));
        cmd.Parameters.AddWithValue("@Id", id.ToString());
        return cmd.ExecuteNonQuery() > 0;
    }

    public Idea? Get(Guid id)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM ideas WHERE Id = @Id";
        cmd.Parameters.AddWithValue("@Id", id.ToString());
        using var reader = cmd.ExecuteReader();
        return reader.Read() ? MapRow(reader) : null;
    }

    public IReadOnlyList<Idea> GetAll(IdeaFilter? filter = null)
    {
        var where = new List<string>();
        var parameters = new List<SqliteParameter>();

        if (!string.IsNullOrWhiteSpace(filter?.Search))
        {
            where.Add("(Title LIKE @Search OR ShortDescription LIKE @Search OR CustomerProblem LIKE @Search OR Keywords LIKE @Search)");
            parameters.Add(new SqliteParameter("@Search", $"%{filter.Search.Trim()}%"));
        }

        if (filter?.ContentType.HasValue == true)
        {
            where.Add("ContentType = @ContentType");
            parameters.Add(new SqliteParameter("@ContentType", (int)filter.ContentType.Value));
        }

        if (filter?.UseCaseSubtype.HasValue == true)
        {
            where.Add("UseCaseSubtype = @UseCaseSubtype");
            parameters.Add(new SqliteParameter("@UseCaseSubtype", (int)filter.UseCaseSubtype.Value));
        }

        if (!string.IsNullOrWhiteSpace(filter?.Category))
        {
            where.Add("Category = @Category");
            parameters.Add(new SqliteParameter("@Category", filter.Category));
        }

        if (filter?.Status.HasValue == true)
        {
            where.Add("Status = @Status");
            parameters.Add(new SqliteParameter("@Status", (int)filter.Status.Value));
        }

        if (!string.IsNullOrWhiteSpace(filter?.PrimaryControl))
        {
            where.Add("PrimaryControl = @PrimaryControl");
            parameters.Add(new SqliteParameter("@PrimaryControl", filter.PrimaryControl));
        }

        if (filter?.PinnedOnly == true)
        {
            where.Add("IsPinned = 1");
        }

        var sql = "SELECT * FROM ideas" +
                  (where.Count > 0 ? " WHERE " + string.Join(" AND ", where) : "") +
                  " ORDER BY IsPinned DESC, UpdatedAt DESC";

        var result = new List<Idea>();
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;
        foreach (var p in parameters)
        {
            cmd.Parameters.Add(p);
        }

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            result.Add(MapRow(reader));
        }

        return result;
    }

    public int Count()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM ideas";
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    // ─────────────────────────── seed ───────────────────────────

    /// <summary>
    /// Loads the embedded sanitized sample ideas. Idempotent: runs only if the
    /// seed has never been applied (tracked by metadata), and skips individual
    /// samples whose Id already exists.
    /// </summary>
    public int LoadSampleIdeasIfMissing()
    {
        if (GetMeta(SampleSeedKey) is not null)
        {
            return 0;
        }

        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .Single(n => n.EndsWith("sample-ideas.json", StringComparison.Ordinal));

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException("Embedded sample-ideas.json not found.");
        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();

        // The embedded sample file is camelCase; deserialize with the matching
        // naming policy so every field and the polymorphic "kind" details
        // payload bind. Enums are stored as integers and deserialize natively.
        var sampleOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };
        var samples = JsonSerializer.Deserialize<List<Idea>>(json, sampleOptions)
            ?? throw new InvalidOperationException("sample-ideas.json is empty or invalid.");

        var added = 0;
        foreach (var sample in samples)
        {
            if (Get(sample.Id) is null)
            {
                Save(sample);
                added++;
            }
        }

        SetMeta(SampleSeedKey, string.Join(",", samples.Select(s => s.Id)));
        return added;
    }

    // ─────────────────────────── mapping ───────────────────────────

    private static void BindIdea(SqliteCommand cmd, Idea idea)
    {
        cmd.Parameters.AddWithValue("@Id", idea.Id.ToString());
        cmd.Parameters.AddWithValue("@Title", idea.Title);
        cmd.Parameters.AddWithValue("@ContentType", (int)idea.ContentType);
        cmd.Parameters.AddWithValue("@UseCaseSubtype", idea.UseCaseSubtype.HasValue ? (object)(int)idea.UseCaseSubtype.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@Category", idea.Category);
        cmd.Parameters.AddWithValue("@Status", (int)idea.Status);
        cmd.Parameters.AddWithValue("@IsPinned", idea.IsPinned ? 1 : 0);
        cmd.Parameters.AddWithValue("@Source", (int)idea.Source);
        cmd.Parameters.AddWithValue("@TargetCustomer", idea.TargetCustomer);
        cmd.Parameters.AddWithValue("@CustomerProblem", idea.CustomerProblem);
        cmd.Parameters.AddWithValue("@ShortDescription", idea.ShortDescription);
        cmd.Parameters.AddWithValue("@PrimaryControl", idea.PrimaryControl);
        cmd.Parameters.AddWithValue("@SupportingControls", idea.SupportingControls);
        cmd.Parameters.AddWithValue("@ControlFeatures", idea.ControlFeatures);
        cmd.Parameters.AddWithValue("@Keywords", idea.Keywords);
        cmd.Parameters.AddWithValue("@WorkflowSummary", idea.WorkflowSummary);
        cmd.Parameters.AddWithValue("@AiOperation", idea.AiOperation);
        cmd.Parameters.AddWithValue("@ScreenNames", idea.ScreenNames);
        cmd.Parameters.AddWithValue("@OriginalUserInput", (object?)idea.OriginalUserInput ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@DetailsJson", idea.Details is null ? (object)DBNull.Value : IdeaDetailsCodec.ToJson(idea.Details));
        cmd.Parameters.AddWithValue("@CreatedAt", idea.CreatedAt.ToString("O"));
        cmd.Parameters.AddWithValue("@UpdatedAt", idea.UpdatedAt.ToString("O"));
    }

    private static Idea MapRow(SqliteDataReader r) => new()
    {
        Id = Guid.Parse(r.GetString(r.GetOrdinal("Id"))),
        Title = r.GetString(r.GetOrdinal("Title")),
        ContentType = (ContentType)r.GetInt32(r.GetOrdinal("ContentType")),
        UseCaseSubtype = r.IsDBNull(r.GetOrdinal("UseCaseSubtype"))
            ? null
            : (UseCaseSubtype)r.GetInt32(r.GetOrdinal("UseCaseSubtype")),
        Category = r.GetString(r.GetOrdinal("Category")),
        Status = (IdeaStatus)r.GetInt32(r.GetOrdinal("Status")),
        IsPinned = r.GetInt32(r.GetOrdinal("IsPinned")) == 1,
        Source = (IdeaSource)r.GetInt32(r.GetOrdinal("Source")),
        TargetCustomer = r.GetString(r.GetOrdinal("TargetCustomer")),
        CustomerProblem = r.GetString(r.GetOrdinal("CustomerProblem")),
        ShortDescription = r.GetString(r.GetOrdinal("ShortDescription")),
        PrimaryControl = r.GetString(r.GetOrdinal("PrimaryControl")),
        SupportingControls = r.GetString(r.GetOrdinal("SupportingControls")),
        ControlFeatures = r.GetString(r.GetOrdinal("ControlFeatures")),
        Keywords = r.GetString(r.GetOrdinal("Keywords")),
        WorkflowSummary = r.GetString(r.GetOrdinal("WorkflowSummary")),
        AiOperation = r.GetString(r.GetOrdinal("AiOperation")),
        ScreenNames = r.GetString(r.GetOrdinal("ScreenNames")),
        OriginalUserInput = r.IsDBNull(r.GetOrdinal("OriginalUserInput")) ? null : r.GetString(r.GetOrdinal("OriginalUserInput")),
        Details = IdeaDetailsCodec.FromJson(r.IsDBNull(r.GetOrdinal("DetailsJson")) ? null : r.GetString(r.GetOrdinal("DetailsJson"))),
        CreatedAt = DateTime.Parse(r.GetString(r.GetOrdinal("CreatedAt")), System.Globalization.CultureInfo.InvariantCulture),
        UpdatedAt = DateTime.Parse(r.GetString(r.GetOrdinal("UpdatedAt")), System.Globalization.CultureInfo.InvariantCulture),
    };

    public void Dispose()
    {
        _connection.Dispose();
    }
}
