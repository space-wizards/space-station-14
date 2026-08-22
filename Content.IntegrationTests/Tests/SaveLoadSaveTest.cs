using System.Collections.Generic;
using System.IO;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.EntitySerialization;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Events;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests
{
    /// <summary>
    /// Tests that a grid's yaml does not change when saved consecutively.
    /// </summary>
    [TestFixture]
    public sealed partial class SaveLoadSaveTest : GameTest
    {
        [Test]
        public async Task CreateSaveLoadSaveGrid()
        {
            var pair = Pair;
            var server = pair.Server;
            var entManager = server.ResolveDependency<IEntityManager>();
            var mapLoader = entManager.System<MapLoaderSystem>();
            var mapSystem = entManager.System<SharedMapSystem>();
            var cfg = server.ResolveDependency<IConfigurationManager>();
            Assume.That(cfg.GetCVar(CCVars.GridFill), Is.False);

            var testSystem = server.System<SaveLoadSaveTestSystem>();
            testSystem.Enabled = true;

            Assume.That(SEntMan.EntityCount.Equals(0), "Lingering entities at the start of CreateSaveLoadSaveGrid");

            var rp1 = new ResPath("/save load save 1.yml");
            var rp2 = new ResPath("/save load save 2.yml");

            MapId mapId0 = MapId.Nullspace;
            MapId mapId1 = MapId.Nullspace;

            await server.WaitPost(() =>
            {
                mapSystem.CreateMap(out mapId0);
                var grid0 = mapSystem.CreateGridEntity(mapId0);
                entManager.RunMapInit(grid0.Owner, entManager.GetComponent<MetaDataComponent>(grid0));
                Assert.That(mapLoader.TrySaveGrid(grid0.Owner, rp1));
                mapSystem.CreateMap(out mapId1);
                Assert.That(mapLoader.TryLoadGrid(mapId1, rp1, out var grid1));
                Assert.That(mapLoader.TrySaveGrid(grid1!.Value, rp2));
            });

            var userData = server.ResolveDependency<IResourceManager>().UserData;

            string one;
            string two;

            await using (var stream = userData.Open(rp1, FileMode.Open))
            using (var reader = new StreamReader(stream))
            {
                one = await reader.ReadToEndAsync();
            }

            await using (var stream = userData.Open(rp2, FileMode.Open))
            using (var reader = new StreamReader(stream))
            {
                two = await reader.ReadToEndAsync();
            }

            Assert.Multiple(() =>
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
            });
            testSystem.Enabled = false;
            await server.WaitPost(() =>
            {
                mapSystem.DeleteMap(mapId0);
                mapSystem.DeleteMap(mapId1);
            });
            Assert.That(SEntMan.EntityCount.Equals(0), "Lingering entities at the end of CreateSaveLoadSaveGrid");
        }

        private new const string TestMap = "Maps/bagel.yml";
        private const string PostInitTestMap = "Maps/saltern.yml";

        /// <summary>
        /// Loads the default map, runs it for 5 ticks, then assert that it did not change.
        /// </summary>
        [Test]
        public async Task LoadSaveTicksSaveBagel()
        {
            var pair = Pair;
            var server = pair.Server;
            var mapLoader = server.ResolveDependency<IEntitySystemManager>().GetEntitySystem<MapLoaderSystem>();
            var mapSys = server.System<SharedMapSystem>();
            var testSystem = server.System<SaveLoadSaveTestSystem>();
            testSystem.Enabled = true;

            Assume.That(SEntMan.EntityCount.Equals(0), "Lingering entities at the start of LoadSaveTicksSaveBagel");

            var rp1 = new ResPath("/load save ticks save 1.yml");
            var rp2 = new ResPath("/load save ticks save 2.yml");

            MapId mapId = default;
            var cfg = server.ResolveDependency<IConfigurationManager>();
            Assert.That(cfg.GetCVar(CCVars.GridFill), Is.False);

            // Load bagel.yml as uninitialized map, and save it to ensure it's up to date.
            await server.WaitPost(() =>
            {
                var path = new ResPath(TestMap);
                Assert.That(mapLoader.TryLoadMap(path, out var map, out _), $"Failed to load test map {TestMap}");
                mapId = map!.Value.Comp.MapId;
                Assert.That(mapLoader.TrySaveMap(mapId, rp1));

                // Run 5 ticks.
                server.RunTicks(5);
            });

            await server.WaitPost(() =>
            {
                Assert.That(mapLoader.TrySaveMap(mapId, rp2));
            });

            var userData = server.ResolveDependency<IResourceManager>().UserData;

            string one;
            string two;

            await using (var stream = userData.Open(rp1, FileMode.Open))
            using (var reader = new StreamReader(stream))
            {
                one = await reader.ReadToEndAsync();
            }

            await using (var stream = userData.Open(rp2, FileMode.Open))
            using (var reader = new StreamReader(stream))
            {
                two = await reader.ReadToEndAsync();
            }

            Assert.Multiple(() =>
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
            });

            testSystem.Enabled = false;
            await server.WaitPost(() => mapSys.DeleteMap(mapId));
            Assert.That(SEntMan.EntityCount.Equals(0), "Lingering entities at the end of LoadSaveTicksSaveBagel");
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
        public async Task LoadTickLoadBagel()
        {
            var pair = Pair;
            var server = pair.Server;

            var mapLoader = server.System<MapLoaderSystem>();
            var mapSys = server.System<SharedMapSystem>();
            var userData = server.ResolveDependency<IResourceManager>().UserData;
            var cfg = server.ResolveDependency<IConfigurationManager>();
            Assume.That(cfg.GetCVar(CCVars.GridFill), Is.False);
            var testSystem = server.System<SaveLoadSaveTestSystem>();
            testSystem.Enabled = true;

            Assume.That(SEntMan.EntityCount.Equals(0), "Lingering entities at the start of LoadTickLoadBagel");

            MapId mapId1 = default;
            MapId mapId2 = default;
            var fileA = new ResPath("/load tick load a.yml");
            var fileB = new ResPath("/load tick load b.yml");
            string yamlA;
            string yamlB;

            // Load & save the first map
            await server.WaitPost(() =>
            {
                var path = new ResPath(TestMap);
                Assert.That(mapLoader.TryLoadMap(path, out var map, out _), $"Failed to load test map {TestMap}");
                mapId1 = map!.Value.Comp.MapId;
                Assert.That(mapLoader.TrySaveMap(mapId1, fileA));
            });

            await using (var stream = userData.Open(fileA, FileMode.Open))
            using (var reader = new StreamReader(stream))
            {
                yamlA = await reader.ReadToEndAsync();
            }

            // Load & save the second map
            await server.WaitPost(() =>
            {
                server.RunTicks(5);

                var path = new ResPath(TestMap);
                Assert.That(mapLoader.TryLoadMap(path, out var map, out _), $"Failed to load test map {TestMap}");
                mapId2 = map!.Value.Comp.MapId;
                Assert.That(mapLoader.TrySaveMap(mapId2, fileB));
            });

            await using (var stream = userData.Open(fileB, FileMode.Open))
            using (var reader = new StreamReader(stream))
            {
                yamlB = await reader.ReadToEndAsync();
            }

            Assert.That(yamlA, Is.EqualTo(yamlB));

            testSystem.Enabled = false;
            await server.WaitPost(() =>
            {
                mapSys.DeleteMap(mapId1);
                mapSys.DeleteMap(mapId2);
            });
            Assert.That(SEntMan.EntityCount.Equals(0), "Lingering entities at the end of LoadTickLoadBagel");
        }

        /// <summary>
        ///     Loads a map, map-initializes it, saves it, then loads and saves that post-mapinit save again.
        /// </summary>
        /// <remarks>
        ///     Saving a post-mapinit map should produce a stable file. This catches systems that mutate serialized
        ///     state while handling post-mapinit entities incorrectly.
        /// </remarks>
        [Test]
        [Explicit("Because master is SO BROKEN it has to be explicit")]
        // Ideally we'd get a global load / postinit / save / load diff test for all entities but baby steps.
        public async Task LoadPostInitSaveLoadSaveSaltern()
        {
            var pair = Pair;
            var server = pair.Server;

            var mapLoader = server.System<MapLoaderSystem>();
            var mapSys = server.System<SharedMapSystem>();
            var userData = server.ResolveDependency<IResourceManager>().UserData;
            var cfg = server.ResolveDependency<IConfigurationManager>();
            Assert.That(cfg.GetCVar(CCVars.GridFill), Is.False);

            var testSystem = server.System<SaveLoadSaveTestSystem>();
            testSystem.Enabled = true;

            var firstSave = new ResPath("/postinit saltern save 1.yml");
            var secondSave = new ResPath("/postinit saltern save 2.yml");
            var initialLoadOptions = DeserializationOptions.Default with
            {
                InitializeMaps = true,
                PauseMaps = true,
                StoreYamlUids = true
            };
            var reloadOptions = DeserializationOptions.Default with
            {
                StoreYamlUids = true
            };

            MapId mapId1 = default;
            MapId mapId2 = default;

            try
            {
                await server.WaitPost(() =>
                {
                    var path = new ResPath(PostInitTestMap);
                    Assert.That(mapLoader.TryLoadMap(path, out var map, out _, initialLoadOptions),
                        $"Failed to load test map {PostInitTestMap}");
                    mapId1 = map!.Value.Comp.MapId;
                    Assert.That(mapSys.IsInitialized(map.Value), Is.True);
                    Assert.That(mapSys.IsPaused(map.Value), Is.True);
                    Assert.That(mapLoader.TrySaveMap(mapId1, firstSave));
                });

                await server.WaitPost(() =>
                {
                    Assert.That(mapLoader.TryLoadMap(firstSave, out var map, out _, reloadOptions),
                        $"Failed to reload post-mapinit save {firstSave}");
                    mapId2 = map!.Value.Comp.MapId;
                    Assert.That(mapSys.IsPaused(map.Value), Is.True);
                    Assert.That(mapLoader.TrySaveMap(mapId2, secondSave));
                });

                await server.WaitIdleAsync();

                var one = await ReadUserData(userData, firstSave);
                var two = await ReadUserData(userData, secondSave);

                Assert.Multiple(() =>
                {
                    if (two != one)
                        TestContext.Error.WriteLine(BuildEntityDiff(mapLoader, firstSave, secondSave));

                    Assert.That(two, Is.EqualTo(one));
                    var failed = TestContext.CurrentContext.Result.Assertions.FirstOrDefault();
                    if (failed != null)
                    {
                        var oneTmp = Path.GetTempFileName();
                        var twoTmp = Path.GetTempFileName();

                        File.WriteAllText(oneTmp, one);
                        File.WriteAllText(twoTmp, two);

                        TestContext.AddTestAttachment(oneTmp, "First post-mapinit save file");
                        TestContext.AddTestAttachment(twoTmp, "Second post-mapinit save file");
                        TestContext.Error.WriteLine("Complete output:");
                        TestContext.Error.WriteLine(oneTmp);
                        TestContext.Error.WriteLine(twoTmp);
                    }
                });
            }
            finally
            {
                testSystem.Enabled = false;
                await server.WaitPost(() =>
                {
                    if (mapSys.MapExists(mapId1))
                        mapSys.DeleteMap(mapId1);

                    if (mapSys.MapExists(mapId2))
                        mapSys.DeleteMap(mapId2);
                });
            }
        }

        private static async Task<string> ReadUserData(IWritableDirProvider userData, ResPath path)
        {
            await using var stream = userData.Open(path, FileMode.Open);
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        }

        private static string BuildEntityDiff(MapLoaderSystem mapLoader, ResPath firstSave, ResPath secondSave)
        {
            if (!mapLoader.TryReadFile(firstSave, out var firstData))
                return $"Failed to read {firstSave} for entity diff.";

            if (!mapLoader.TryReadFile(secondSave, out var secondData))
                return $"Failed to read {secondSave} for entity diff.";

            var firstEntities = ReadSavedEntities(firstData);
            var secondEntities = ReadSavedEntities(secondData);

            var removed = firstEntities.Keys.Except(secondEntities.Keys).Order().ToArray();
            var added = secondEntities.Keys.Except(firstEntities.Keys).Order().ToArray();
            var changed = firstEntities.Keys
                .Intersect(secondEntities.Keys)
                .Where(uid => firstEntities[uid].Text != secondEntities[uid].Text)
                .Order()
                .ToArray();

            using var writer = new StringWriter();
            writer.WriteLine("Post-mapinit save/load/save entity diff:");
            writer.WriteLine($"  First save entities: {firstEntities.Count}");
            writer.WriteLine($"  Second save entities: {secondEntities.Count}");
            writer.WriteLine($"  Added: {added.Length}");
            writer.WriteLine($"  Removed: {removed.Length}");
            writer.WriteLine($"  Changed: {changed.Length}");
            WriteEntityList(writer, "Added entities", added, secondEntities);
            WriteEntityList(writer, "Removed entities", removed, firstEntities);
            WriteChangedEntityList(writer, changed, firstEntities, secondEntities);
            return writer.ToString();
        }

        private static Dictionary<int, SavedEntity> ReadSavedEntities(MappingDataNode root)
        {
            var entities = new Dictionary<int, SavedEntity>();
            var prototypeGroups = root.Get<SequenceDataNode>("entities");

            foreach (var protoGroup in prototypeGroups.Cast<MappingDataNode>())
            {
                var proto = protoGroup.Get<ValueDataNode>("proto").Value;
                var groupEntities = protoGroup.Get<SequenceDataNode>("entities");

                foreach (var entityNode in groupEntities.Cast<MappingDataNode>())
                {
                    var uid = entityNode.Get<ValueDataNode>("uid").AsInt();
                    entities[uid] = new SavedEntity(uid, proto, entityNode.ToString(), ReadComponents(entityNode));
                }
            }

            return entities;
        }

        private static Dictionary<string, string> ReadComponents(MappingDataNode entityNode)
        {
            if (!entityNode.TryGet("components", out SequenceDataNode? components))
                return new Dictionary<string, string>();

            var result = new Dictionary<string, string>();
            foreach (var component in components.Cast<MappingDataNode>())
            {
                var type = component.Get<ValueDataNode>("type").Value;
                result[type] = component.ToString();
            }

            return result;
        }

        private static void WriteEntityList(
            TextWriter writer,
            string title,
            int[] uids,
            Dictionary<int, SavedEntity> entities)
        {
            if (uids.Length == 0)
                return;

            const int limit = 50;
            writer.WriteLine($"  {title}:");
            foreach (var uid in uids.Take(limit))
            {
                writer.WriteLine($"    {DescribeEntity(entities[uid])}");
            }

            if (uids.Length > limit)
                writer.WriteLine($"    ... {uids.Length - limit} more");
        }

        private static void WriteChangedEntityList(
            TextWriter writer,
            int[] uids,
            Dictionary<int, SavedEntity> firstEntities,
            Dictionary<int, SavedEntity> secondEntities)
        {
            if (uids.Length == 0)
                return;

            const int limit = 50;
            writer.WriteLine("  Changed entities:");
            foreach (var uid in uids.Take(limit))
            {
                var first = firstEntities[uid];
                var second = secondEntities[uid];
                writer.WriteLine($"    {DescribeEntity(first)} -> {DescribeEntity(second)}");
                WriteComponentDiff(writer, first, second);
            }

            if (uids.Length > limit)
                writer.WriteLine($"    ... {uids.Length - limit} more");
        }

        private static void WriteComponentDiff(TextWriter writer, SavedEntity first, SavedEntity second)
        {
            var firstComponents = first.Components;
            var secondComponents = second.Components;
            var removed = firstComponents.Keys.Except(secondComponents.Keys).Order().ToArray();
            var added = secondComponents.Keys.Except(firstComponents.Keys).Order().ToArray();
            var changed = firstComponents.Keys
                .Intersect(secondComponents.Keys)
                .Where(type => firstComponents[type] != secondComponents[type])
                .Order()
                .ToArray();

            if (first.Proto != second.Proto)
                writer.WriteLine($"      proto: {first.Proto} -> {second.Proto}");

            if (added.Length != 0)
                writer.WriteLine($"      added components: {string.Join(", ", added)}");

            if (removed.Length != 0)
                writer.WriteLine($"      removed components: {string.Join(", ", removed)}");

            if (changed.Length != 0)
                writer.WriteLine($"      changed components: {string.Join(", ", changed)}");
        }

        private static string DescribeEntity(SavedEntity entity)
            => $"{entity.Uid} ({entity.Proto})";

        private sealed record SavedEntity(
            int Uid,
            string Proto,
            string Text,
            Dictionary<string, string> Components);

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
}
