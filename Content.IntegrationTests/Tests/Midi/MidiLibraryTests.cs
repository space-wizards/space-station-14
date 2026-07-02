#nullable enable

using System.IO;
using System.Linq;
using Content.Client.Midi;
using Content.IntegrationTests.Fixtures;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests.Midi;

[TestFixture]
public sealed partial class MidiLibraryTests : GameTest
{
    private static readonly byte[] TestBytes = [1, 2, 3, 4, 5, 6];
    private const string TestFileName = "unit_test.midi";
    private static ResPath TestUserDataDir => new ResPath("/UserMidis/");
    private static ResPath TestFullPath => new ResPath(TestUserDataDir + TestFileName);

    private IResourceManager ResManager => Pair.Client.ResolveDependency<IResourceManager>();
    private MidiLibraryManager MidiLibManager => Pair.Client.ResolveDependency<MidiLibraryManager>();

    [TearDown]
    public void CleanUserData()
    {
        foreach (var file in ResManager.UserData.DirectoryEntries(TestUserDataDir))
        {
            ResManager.UserData.Delete(new ResPath(TestUserDataDir + file));
        }
        MidiLibManager.ReloadLibrary();
    }

    [Test]
    public async Task TestAddMidiFile()
    {
        var addedFileName = "";
        Stream stream = new MemoryStream(TestBytes);
        MidiLibManager.MidiFileAdded += s => { addedFileName = s; };

        await MidiLibManager.AddMidiFile(TestFileName, stream);
        var outputBytes = ResManager.UserData.ReadAllBytes(TestFullPath);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(MidiLibManager.GetMidiFiles(), Contains.Item(TestFileName));
            Assert.That(outputBytes, Is.EqualTo(TestBytes));
            Assert.That(addedFileName, Is.EqualTo(TestFileName));
        }
    }

    [Test]
    public void TestGetMidiData()
    {

        ResManager.UserData.WriteAllBytes(TestFullPath, TestBytes);
        var midiBytes = MidiLibManager.GetMidiData(TestFileName);

        Assert.That(TestBytes, Is.EqualTo(midiBytes));
    }

    [Test]
    public void TestRemoveMidiFile()
    {
        var removedFileName = "";
        MidiLibManager.MidiFileRemoved += s => { removedFileName = s; };

        ResManager.UserData.WriteAllBytes(TestFullPath, TestBytes);
        Assert.That(ResManager.UserData.Exists(TestFullPath), Is.True);

        MidiLibManager.RemoveMidiFile(TestFileName);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ResManager.UserData.Exists(TestFullPath), Is.False);
            Assert.That(MidiLibManager.GetMidiData(TestFileName), Is.Empty);
            Assert.That(MidiLibManager.GetMidiFiles(), Is.Empty);
            Assert.That(removedFileName, Is.EqualTo(TestFileName));
        }
    }

    [Test]
    public async Task TestRemoveAllMidiFiles()
    {
        var resetFired = false;

        MidiLibManager.MidiFilesReset += () => { resetFired = true; };
        await MidiLibManager.AddMidiFile("1_" + TestFileName, TestBytes);
        await MidiLibManager.AddMidiFile("2_" + TestFileName, TestBytes);
        await MidiLibManager.AddMidiFile("3_" + TestFileName, TestBytes);

        Assert.That(MidiLibManager.GetMidiFiles().Count(), Is.EqualTo(3));

        MidiLibManager.RemoveAllMidiFiles();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(MidiLibManager.GetMidiFiles(), Is.Empty);
            Assert.That(resetFired, Is.True);
        }
    }

    [Test]
    public void TestRenameMidiFile()
    {
        const string renamedFileName = "unit_test_renamed.midi";
        var removedFileName = "";
        var addedFileName = "";

        MidiLibManager.MidiFileRemoved += s => { removedFileName = s; };
        MidiLibManager.MidiFileAdded += s => { addedFileName = s; };

        ResManager.UserData.WriteAllBytes(TestFullPath, TestBytes);
        Assert.That(ResManager.UserData.Exists(TestFullPath), Is.True);

        MidiLibManager.RenameMidiFile(TestFileName, renamedFileName);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(ResManager.UserData.Exists(new ResPath(TestUserDataDir + renamedFileName)), Is.True);
            Assert.That(ResManager.UserData.Exists(TestFullPath), Is.False);
            Assert.That(removedFileName, Is.EqualTo(TestFileName));
            Assert.That(addedFileName, Is.EqualTo(renamedFileName));
        }
    }
}
