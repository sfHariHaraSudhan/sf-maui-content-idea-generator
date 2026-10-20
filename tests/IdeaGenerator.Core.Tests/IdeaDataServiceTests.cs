using IdeaGenerator.Core.Models;
using IdeaGenerator.Core.Services;
using Xunit;

namespace IdeaGenerator.Core.Tests;

/// <summary>
/// Phase 1 focused tests. No other tests may be added without a demonstrated
/// Phase 1 defect (frozen plan).
/// </summary>
public sealed class IdeaDataServiceTests : IDisposable
{
    private readonly string _dbPath;

    public IdeaDataServiceTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"ideagen-test-{Guid.NewGuid():N}.sqlite");
    }

    public void Dispose()
    {
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }

    private static Idea CreateIdea(string title = "Test idea", IdeaStatus status = IdeaStatus.Planned) => new()
    {
        Title = title,
        ContentType = ContentType.UseCaseSample,
        UseCaseSubtype = UseCaseSubtype.WithoutAi,
        Category = Categories.Productivity,
        Status = status,
        PrimaryControl = "DataGrid",
        ShortDescription = "A sample idea for tests.",
        Details = new UseCaseWithoutAiDetails
        {
            PracticalWorkflow = "Step one, step two.",
            ExpectedEffort = "1 to 2 working days",
        },
    };

    [Fact]
    public void Save_and_retrieve_an_idea()
    {
        using var service = new IdeaDataService(_dbPath);
        var idea = CreateIdea("Save and retrieve");

        service.Save(idea);

        var loaded = service.Get(idea.Id);
        Assert.NotNull(loaded);
        Assert.Equal(idea.Title, loaded!.Title);
        Assert.Equal(idea.Category, loaded.Category);
        Assert.Equal(idea.Status, loaded.Status);
        Assert.Equal(UseCaseSubtype.WithoutAi, loaded.UseCaseSubtype);
        Assert.NotNull(loaded.Details);
        Assert.IsType<UseCaseWithoutAiDetails>(loaded.Details);
    }

    [Fact]
    public void Pin_and_unpin_an_idea()
    {
        using var service = new IdeaDataService(_dbPath);
        var idea = CreateIdea();
        service.Save(idea);

        Assert.False(service.Get(idea.Id)!.IsPinned);

        Assert.True(service.SetPinned(idea.Id, pinned: true));
        Assert.True(service.Get(idea.Id)!.IsPinned);

        Assert.True(service.SetPinned(idea.Id, pinned: false));
        Assert.False(service.Get(idea.Id)!.IsPinned);
    }

    [Fact]
    public void Generated_candidate_remains_transient_until_explicitly_saved()
    {
        using var service = new IdeaDataService(_dbPath);
        var candidate = CreateIdea("Transient candidate");

        // A generated candidate is only an in-memory object until Save is called.
        Assert.Null(service.Get(candidate.Id));
        Assert.Empty(service.GetAll());

        service.Save(candidate);

        Assert.NotNull(service.Get(candidate.Id));
        Assert.Equal(1, service.Count());
    }

    [Fact]
    public void Saved_data_survives_database_reopen()
    {
        Idea idea;
        using (var service = new IdeaDataService(_dbPath))
        {
            idea = CreateIdea("Survives reopen");
            service.Save(idea);
            service.SetPinned(idea.Id, pinned: true);
        }

        using (var reopened = new IdeaDataService(_dbPath))
        {
            var loaded = reopened.Get(idea.Id);
            Assert.NotNull(loaded);
            Assert.Equal("Survives reopen", loaded!.Title);
            Assert.True(loaded.IsPinned);
            Assert.NotNull(loaded.Details);
        }
    }
}
