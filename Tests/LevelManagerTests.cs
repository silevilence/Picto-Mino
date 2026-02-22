using PictoMino.Core;

namespace PictoMino.Tests;

[TestFixture]
public class LevelManagerTests
{
    private LevelManager CreateManagerWithTutorial()
    {
        var manager = new LevelManager();
        manager.AddChapter(LevelManager.CreateTutorialChapter());
        return manager;
    }

    [Test]
    public void CreateTutorialChapter_CreatesValidChapter()
    {
        var chapter = LevelManager.CreateTutorialChapter();

        Assert.That(chapter.Id, Is.EqualTo("tutorial"));
        Assert.That(chapter.Levels.Length, Is.GreaterThan(0));
    }

    [Test]
    public void AddChapter_RegistersLevels()
    {
        var manager = CreateManagerWithTutorial();

        Assert.That(manager.Chapters.Count, Is.EqualTo(1));
        Assert.That(manager.TotalLevelCount, Is.GreaterThan(0));
    }

    [Test]
    public void GetLevel_ValidId_ReturnsLevel()
    {
        var manager = CreateManagerWithTutorial();

        var level = manager.GetLevel("tutorial_01");

        Assert.That(level, Is.Not.Null);
        Assert.That(level!.Name, Is.EqualTo("初识拼图"));
    }

    [Test]
    public void GetLevel_InvalidId_ReturnsNull()
    {
        var manager = CreateManagerWithTutorial();

        var level = manager.GetLevel("nonexistent");

        Assert.That(level, Is.Null);
    }

    [Test]
    public void IsUnlocked_FirstLevel_AlwaysUnlocked()
    {
        var manager = CreateManagerWithTutorial();

        Assert.That(manager.IsUnlocked("tutorial_01"), Is.True);
    }

    [Test]
    public void IsUnlocked_SecondLevel_RequiresFirstCompleted()
    {
        var manager = CreateManagerWithTutorial();

        Assert.That(manager.IsUnlocked("tutorial_02"), Is.False);

        manager.UpdateProgress("tutorial_01", true);
        Assert.That(manager.IsUnlocked("tutorial_02"), Is.True);
    }

    [Test]
    public void UpdateProgress_TracksCompletion()
    {
        var manager = CreateManagerWithTutorial();

        manager.UpdateProgress("tutorial_01", true, 15.5f);
        var progress = manager.GetProgress("tutorial_01");

        Assert.That(progress.IsCompleted, Is.True);
        Assert.That(progress.BestTime, Is.EqualTo(15.5f));
        Assert.That(progress.PlayCount, Is.EqualTo(1));
    }

    [Test]
    public void UpdateProgress_KeepsBestTime()
    {
        var manager = CreateManagerWithTutorial();

        manager.UpdateProgress("tutorial_01", true, 20f);
        manager.UpdateProgress("tutorial_01", true, 15f);
        manager.UpdateProgress("tutorial_01", true, 25f);
        var progress = manager.GetProgress("tutorial_01");

        Assert.That(progress.BestTime, Is.EqualTo(15f));
        Assert.That(progress.PlayCount, Is.EqualTo(3));
    }

    [Test]
    public void GetNextLevelId_ReturnsCorrectNext()
    {
        var manager = CreateManagerWithTutorial();

        var next = manager.GetNextLevelId("tutorial_01");

        Assert.That(next, Is.EqualTo("tutorial_02"));
    }

    [Test]
    public void GetNextLevelId_LastLevel_ReturnsNull()
    {
        var manager = CreateManagerWithTutorial();
        var chapter = manager.Chapters[0];
        var lastId = chapter.Levels[^1].Id;

        var next = manager.GetNextLevelId(lastId);

        Assert.That(next, Is.Null);
    }

    [Test]
    public void CompletedLevelCount_TracksCorrectly()
    {
        var manager = CreateManagerWithTutorial();

        Assert.That(manager.CompletedLevelCount, Is.EqualTo(0));

        manager.UpdateProgress("tutorial_01", true);
        Assert.That(manager.CompletedLevelCount, Is.EqualTo(1));

        manager.UpdateProgress("tutorial_02", true);
        Assert.That(manager.CompletedLevelCount, Is.EqualTo(2));
    }

    [Test]
    public void ResetAllProgress_ClearsEverything()
    {
        var manager = CreateManagerWithTutorial();
        manager.UpdateProgress("tutorial_01", true);
        manager.UpdateProgress("tutorial_02", true);

        manager.ResetAllProgress();

        Assert.That(manager.CompletedLevelCount, Is.EqualTo(0));
    }

    [Test]
    public void OnProgressUpdated_FiresOnUpdate()
    {
        var manager = CreateManagerWithTutorial();
        string firedId = "";
        LevelProgress? firedProgress = null;
        manager.OnProgressUpdated += (id, p) => { firedId = id; firedProgress = p; };

        manager.UpdateProgress("tutorial_01", true, 10f);

        Assert.That(firedId, Is.EqualTo("tutorial_01"));
        Assert.That(firedProgress!.IsCompleted, Is.True);
    }
}
