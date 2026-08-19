#nullable enable

using System.IO;
using System.Linq;
using Content.Client.Audio.Midi;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests.Audio.Midi;

public sealed partial class MidiFileCollectionManagerTests : GameTest
{
    private static readonly byte[] TestBytes = [1, 2, 3, 4, 5, 6];
    private static readonly ResPath TestFileName = new("unit_test.midi");
    private static ResPath TestUserDataDir => new("/UserMidis/");
    private static ResPath TestFullPath => TestUserDataDir / TestFileName;

    [SidedDependency(Side.Client)] private IResourceManager _cResManager = default!;
    [SidedDependency(Side.Client)] private MidiFileCollectionManager _cMidiLibManager = default!;

    [TearDown]
    public void CleanUserData()
    {
        foreach (var file in _cResManager.UserData.DirectoryEntries(TestUserDataDir))
        {
            _cResManager.UserData.Delete(new ResPath(TestUserDataDir + file));
        }

        _cMidiLibManager.ReloadLibrary();
    }

    [Test]
    public async Task TestAddMidiFile()
    {
        var addedFileName = new ResPath("");
        Stream stream = new MemoryStream(TestBytes);
        _cMidiLibManager.MidiFileAdded += s => { addedFileName = s; };

        await _cMidiLibManager.AddMidiFile(TestFileName, stream);
        var outputBytes = _cResManager.UserData.ReadAllBytes(TestFullPath);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_cMidiLibManager.GetMidiFiles(), Contains.Item(TestFileName));
            Assert.That(outputBytes, Is.EqualTo(TestBytes));
            Assert.That(addedFileName, Is.EqualTo(TestFileName));
        }
    }

    [Test]
    public void TestGetMidiData()
    {
        _cResManager.UserData.WriteAllBytes(TestFullPath, TestBytes);
        var midiBytes = _cMidiLibManager.GetMidiData(TestFileName);

        Assert.That(TestBytes, Is.EqualTo(midiBytes));
    }

    [Test]
    public void TestRemoveMidiFile()
    {
        var removedFileName = new ResPath("");
        _cMidiLibManager.MidiFileRemoved += s => { removedFileName = s; };

        _cResManager.UserData.WriteAllBytes(TestFullPath, TestBytes);
        Assert.That(_cResManager.UserData.Exists(TestFullPath), Is.True);

        _cMidiLibManager.RemoveMidiFile(TestFileName);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_cResManager.UserData.Exists(TestFullPath), Is.False);
            Assert.That(_cMidiLibManager.GetMidiFiles(), Is.Empty);
            Assert.That(removedFileName, Is.EqualTo(TestFileName));
        }
    }

    [Test]
    public async Task TestRemoveAllMidiFiles()
    {
        var resetFired = false;

        _cMidiLibManager.MidiFilesReset += () => { resetFired = true; };
        await _cMidiLibManager.AddMidiFile(new ResPath("1_unit_test.midi"), TestBytes);
        await _cMidiLibManager.AddMidiFile(new ResPath("2_unit_test.midi"), TestBytes);
        await _cMidiLibManager.AddMidiFile(new ResPath("3_unit_test.midi"), TestBytes);

        Assert.That(_cMidiLibManager.GetMidiFiles().Count(), Is.EqualTo(3));

        _cMidiLibManager.RemoveAllMidiFiles();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_cMidiLibManager.GetMidiFiles(), Is.Empty);
            Assert.That(resetFired, Is.True);
        }
    }

    [Test]
    public void TestRenameMidiFile()
    {
        var renamedFileName = new ResPath("unit_test_renamed.midi");
        var removedFileName = new ResPath("");
        var addedFileName = new ResPath("");

        _cMidiLibManager.MidiFileRemoved += s => { removedFileName = s; };
        _cMidiLibManager.MidiFileAdded += s => { addedFileName = s; };

        _cResManager.UserData.WriteAllBytes(TestFullPath, TestBytes);
        Assert.That(_cResManager.UserData.Exists(TestFullPath), Is.True);

        _cMidiLibManager.RenameMidiFile(TestFileName, renamedFileName);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_cResManager.UserData.Exists(TestUserDataDir / renamedFileName), Is.True);
            Assert.That(_cResManager.UserData.Exists(TestFullPath), Is.False);
            Assert.That(removedFileName, Is.EqualTo(TestFileName));
            Assert.That(addedFileName, Is.EqualTo(renamedFileName));
        }
    }
}
