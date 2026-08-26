#nullable enable
using System.IO;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Events;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests;

/// <summary>
/// Tests that a grid's yaml does not change when saved consecutively.
/// </summary>
public sealed partial class SaveLoadSaveTest : GameTest
{
    [SidedDependency(Side.Server)] private MapLoaderSystem _sMapLoader = default!;
    [SidedDependency(Side.Server)] private SharedMapSystem _sMap = default!;
    [SidedDependency(Side.Server)] private IConfigurationManager _sCfg = default!;
    [SidedDependency(Side.Server)] private IResourceManager _sResMan = default!;
    [SidedDependency(Side.Server)] private SaveLoadSaveTestSystem _sTest = default!;

    [Test]
    [RunOnSide(Side.Server)]
    [Description("Tries to save and load a simple grid.")]
    public async Task CreateSaveLoadSaveGrid()
    {
        Assume.That(_sCfg.GetCVar(CCVars.GridFill), Is.False);

        _sTest.Enabled = true;

        Assume.That(SEntMan.EntityCount, Is.Zero, $"Lingering entities at the start of {nameof(CreateSaveLoadSaveGrid)}");

        var rp1 = new ResPath("/save load save 1.yml");
        var rp2 = new ResPath("/save load save 2.yml");

        _sMap.CreateMap(out var mapId0);
        var grid0 = _sMap.CreateGridEntity(mapId0);
        SEntMan.RunMapInit(grid0.Owner, SComp<MetaDataComponent>(grid0));
        Assert.That(_sMapLoader.TrySaveGrid(grid0.Owner, rp1));
        _sMap.CreateMap(out var mapId1);
        Assert.That(_sMapLoader.TryLoadGrid(mapId1, rp1, out var grid1));
        Assert.That(_sMapLoader.TrySaveGrid(grid1!.Value, rp2));

        var userData = _sResMan.UserData;

        string one;
        string two;

        using (var reader = new StreamReader(userData.Open(rp1, FileMode.Open)))
        {
            one = reader.ReadToEnd();
        }

        using (var reader = new StreamReader(userData.Open(rp2, FileMode.Open)))
        {
            two = reader.ReadToEnd();
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(two, Has.Length.Positive);
            Assert.That(two, Is.EqualTo(one));
            var failed = TestContext.CurrentContext.Result.Assertions.FirstOrDefault();
            if (failed != null)
            {
                var oneTmp = Path.GetTempFileName();
                var twoTmp = Path.GetTempFileName();

                File.WriteAllText(oneTmp, one);
                File.WriteAllText(twoTmp, two);

                TestContext.AddTestAttachment(oneTmp, "First save file");
                TestContext.AddTestAttachment(twoTmp, "Second save file");
                TestContext.Error.WriteLine("Complete output:");
                TestContext.Error.WriteLine(oneTmp);
                TestContext.Error.WriteLine(twoTmp);
            }
        }
        _sTest.Enabled = false;

        _sMap.DeleteMap(mapId0);
        _sMap.DeleteMap(mapId1);
        Assert.That(SEntMan.EntityCount, Is.Zero, $"Lingering entities at the end of {nameof(CreateSaveLoadSaveGrid)}");
    }

    private new const string TestMap = "Maps/bagel.yml";

    /// <summary>
    /// Loads the default map, runs it for 5 ticks, then assert that it did not change.
    /// </summary>
    [Test]
    [RunOnSide(Side.Server)]
    public async Task LoadSaveTicksSaveBagel()
    {
        _sTest.Enabled = true;

        Assume.That(SEntMan.EntityCount, Is.Zero, $"Lingering entities at the start of {nameof(LoadSaveTicksSaveBagel)}");

        var rp1 = new ResPath("/load save ticks save 1.yml");
        var rp2 = new ResPath("/load save ticks save 2.yml");

        MapId mapId = default;
        Assert.That(_sCfg.GetCVar(CCVars.GridFill), Is.False);

        // Load bagel.yml as uninitialized map, and save it to ensure it's up to date.
        var path = new ResPath(TestMap);
        Assert.That(_sMapLoader.TryLoadMap(path, out var map, out _), $"Failed to load test map {TestMap}");
        mapId = map!.Value.Comp.MapId;
        Assert.That(_sMapLoader.TrySaveMap(mapId, rp1));

        // Run 5 ticks.
        Server.RunTicks(5);

        Assert.That(_sMapLoader.TrySaveMap(mapId, rp2));

        var userData = _sResMan.UserData;

        string one;
        string two;

        using (var reader = new StreamReader(userData.Open(rp1, FileMode.Open)))
        {
            one = reader.ReadToEnd();
        }

        using (var reader = new StreamReader(userData.Open(rp2, FileMode.Open)))
        {
            two = reader.ReadToEnd();
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(two, Is.EqualTo(one));
            var failed = TestContext.CurrentContext.Result.Assertions.FirstOrDefault();
            if (failed != null)
            {
                var oneTmp = Path.GetTempFileName();
                var twoTmp = Path.GetTempFileName();

                File.WriteAllText(oneTmp, one);
                File.WriteAllText(twoTmp, two);

                TestContext.AddTestAttachment(oneTmp, "First save file");
                TestContext.AddTestAttachment(twoTmp, "Second save file");
                TestContext.Error.WriteLine("Complete output:");
                TestContext.Error.WriteLine(oneTmp);
                TestContext.Error.WriteLine(twoTmp);
            }
        }

        _sTest.Enabled = false;
        _sMap.DeleteMap(mapId);
        Assert.That(SEntMan.EntityCount, Is.Zero, $"Lingering entities at the end of {nameof(LoadSaveTicksSaveBagel)}");
    }

    /// <summary>
    /// Loads the same uninitialized map at slightly different times, and then checks that they are the same
    /// when getting saved.
    /// </summary>
    /// <remarks>
    /// Should ensure that entities do not perform randomization prior to initialization and should prevents
    /// bugs like the one discussed in github.com/space-wizards/RobustToolbox/issues/3870. This test is somewhat
    /// similar to <see cref="LoadSaveTicksSaveBagel"/> and <see cref="SaveLoadSave"/>, but neither of these
    /// caught the mentioned bug.
    /// </remarks>
    [Test]
    [RunOnSide(Side.Server)]
    [Description("Saves Bagel multiple times, checking that the YAML is identical between the two.")]
    public async Task LoadTickLoadBagel()
    {
        var userData = Server.ResolveDependency<IResourceManager>().UserData;
        Assume.That(_sCfg.GetCVar(CCVars.GridFill), Is.False);
        _sTest.Enabled = true;

        Assume.That(SEntMan.EntityCount, Is.Zero, $"Lingering entities at the start of {nameof(LoadTickLoadBagel)}");

        var fileA = new ResPath("/load tick load a.yml");
        var fileB = new ResPath("/load tick load b.yml");
        string yamlA;
        string yamlB;

        // Load & save the first map
        var path = new ResPath(TestMap);
        Assert.That(_sMapLoader.TryLoadMap(path, out var map, out _), $"Failed to load test map {TestMap}");
        var mapId1 = map!.Value.Comp.MapId;
        Assert.That(_sMapLoader.TrySaveMap(mapId1, fileA));

        using (var reader = new StreamReader(userData.Open(fileA, FileMode.Open)))
        {
            yamlA = reader.ReadToEnd();
        }

        // Load & save the second map
        Server.RunTicks(5);

        path = new ResPath(TestMap);
        Assert.That(_sMapLoader.TryLoadMap(path, out map, out _), $"Failed to load test map {TestMap}");
        var mapId2 = map!.Value.Comp.MapId;
        Assert.That(_sMapLoader.TrySaveMap(mapId2, fileB));

        using (var reader = new StreamReader(userData.Open(fileB, FileMode.Open)))
        {
            yamlB = reader.ReadToEnd();
        }

        Assert.That(yamlA, Has.Length.Positive);
        Assert.That(yamlA, Is.EqualTo(yamlB));

        _sTest.Enabled = false;
        _sMap.DeleteMap(mapId1);
        _sMap.DeleteMap(mapId2);
        Assert.That(SEntMan.EntityCount, Is.Zero, $"Lingering entities at the end of {nameof(LoadTickLoadBagel)}");
    }

    /// <summary>
    /// Simple system that modifies the data saved to a yaml file by removing the timestamp.
    /// Required by some tests that validate that re-saving a map does not modify it.
    /// </summary>
    private sealed partial class SaveLoadSaveTestSystem : EntitySystem
    {
        public bool Enabled;
        public override void Initialize()
        {
            SubscribeLocalEvent<AfterSerializationEvent>(OnAfterSave);
        }

        private void OnAfterSave(AfterSerializationEvent ev)
        {
            if (!Enabled)
                return;

            // Remove timestamp.
            ((MappingDataNode)ev.Node["meta"]).Remove("time");
        }
    }
}
