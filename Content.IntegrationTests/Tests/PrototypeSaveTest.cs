#nullable enable
using System.Collections.Generic;
using System.Linq;
using Content.Shared.Coordinates;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Content.IntegrationTests.Tests;

/// <summary>
///     This test ensure that when an entity prototype is spawned into an un-initialized map, its component data is not
///     modified during init. I.e., when the entity is saved to the map, its data is simply the default prototype data (ignoring transform component).
/// </summary>
/// <remarks>
///     If you are here because this test is failing on your PR, then one easy way of figuring out how to fix the prototype is to just
///     spawn it into a new empty map and seeing what the map yml looks like.
/// </remarks>
[TestFixture]
public sealed class PrototypeSaveTest
{
    [Test]
    public async Task UninitializedSaveTest()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var mapManager = server.ResolveDependency<IMapManager>();
        var entityMan = server.ResolveDependency<IEntityManager>();
        var prototypeMan = server.ResolveDependency<IPrototypeManager>();
        var tileDefinitionManager = server.ResolveDependency<ITileDefinitionManager>();
        var seriMan = server.ResolveDependency<ISerializationManager>();
        var compFact = server.ResolveDependency<IComponentFactory>();

        var prototypes = new List<EntityPrototype>();
        MapGridComponent grid = default!;
        EntityUid uid;
        MapId mapId = default;

        //Build up test environment
        await server.WaitPost(() =>
        {
            // Create a one tile grid to stave off the grid 0 monsters
            mapId = mapManager.CreateMap();

            mapManager.AddUninitializedMap(mapId);

            grid = mapManager.CreateGrid(mapId);

            var tileDefinition = tileDefinitionManager["FloorSteel"]; // Wires n such disable ambiance while under the floor
            var tile = new Tile(tileDefinition.TileId);
            var coordinates = grid.ToCoordinates();

            grid.SetTile(coordinates, tile);
        });

        await server.WaitRunTicks(5);

        //Generate list of non-abstract prototypes to test
        foreach (var prototype in prototypeMan.EnumeratePrototypes<EntityPrototype>())
        {
            if (prototype.Abstract)
                continue;

            if (pair.IsTestPrototype(prototype))
                continue;

            // Yea this test just doesn't work with this, it parents a grid to another grid and causes game logic to explode.
            if (prototype.Components.ContainsKey("MapGrid"))
                continue;

            // Currently mobs and such can't be serialized, but they aren't flagged as serializable anyways.
            if (!prototype.MapSavable)
                continue;

            if (prototype.SetSuffix == "DEBUG")
                continue;

            prototypes.Add(prototype);
        }

        var context = new TestEntityUidContext();

        await server.WaitAssertion(() =>
        {
            Assert.That(!mapManager.IsMapInitialized(mapId));
            var testLocation = grid.ToCoordinates();

            Assert.Multiple(() =>
            {
                //Iterate list of prototypes to spawn
                foreach (var prototype in prototypes)
                {
                    uid = entityMan.SpawnEntity(prototype.ID, testLocation);
                    context.Prototype = prototype;

                    // get default prototype data
                    Dictionary<string, MappingDataNode> protoData = new();
                    try
                    {
                        context.WritingReadingPrototypes = true;

                        foreach (var (compType, comp) in prototype.Components)
                        {
                            context.WritingComponent = compType;
                            protoData.Add(compType, seriMan.WriteValueAs<MappingDataNode>(comp.Component.GetType(), comp.Component, alwaysWrite: true, context: context));
                        }

                        context.WritingComponent = string.Empty;
                        context.WritingReadingPrototypes = false;
                    }
                    catch (Exception e)
                    {
                        Assert.Fail($"Failed to convert prototype {prototype.ID} into yaml. Exception: {e.Message}");
                        continue;
                    }

                    var comps = new HashSet<IComponent>(entityMan.GetComponents(uid));
                    var compNames = new HashSet<string>(comps.Count);
                    foreach (var component in comps)
                    {
                        var compType = component.GetType();
                        var compName = compFact.GetComponentName(compType);
                        compNames.Add(compName);

                        if (compType == typeof(MetaDataComponent) || compType == typeof(TransformComponent) || compType == typeof(FixturesComponent))
                            continue;

                        MappingDataNode compMapping;
                        try
                        {
                            context.WritingComponent = compName;
                            compMapping = seriMan.WriteValueAs<MappingDataNode>(compType, component, alwaysWrite: true, context: context);
                        }
                        catch (Exception e)
                        {
                            Assert.Fail($"Failed to serialize {compName} component of entity prototype {prototype.ID}. Exception: {e.Message}");
                            continue;
                        }

                        if (protoData.TryGetValue(compName, out var protoMapping))
                        {
                            var diff = compMapping.Except(protoMapping);

                            if (diff != null && diff.Children.Count != 0)
                                Assert.Fail($"Prototype {prototype.ID} modifies component on spawn: {compName}. Modified yaml:\n{diff}");
                        }
                        else
                        {
                            Assert.Fail($"Prototype {prototype.ID} gains a component on spawn: {compName}");
                        }
                    }

                    // An entity may also remove components on init -> check no components are missing.
                    foreach (var compType in prototype.Components.Keys)
                    {
                        Assert.That(compNames, Does.Contain(compType), $"Prototype {prototype.ID} removes component {compType} on spawn.");
                    }

                    if (!entityMan.Deleted(uid))
                        entityMan.DeleteEntity(uid);
                }
            });
        });
        await pair.CleanReturnAsync();
    }

    public sealed class TestEntityUidContext : ISerializationContext,
        ITypeSerializer<EntityUid, ValueDataNode>
    {
        public SerializationManager.SerializerProvider SerializerProvider { get; }
        public bool WritingReadingPrototypes { get; set; }

        public string WritingComponent = string.Empty;
        public EntityPrototype? Prototype;

        public TestEntityUidContext()
        {
            SerializerProvider = new();
            SerializerProvider.RegisterSerializer(this);
        }

        ValidationNode ITypeValidator<EntityUid, ValueDataNode>.Validate(ISerializationManager serializationManager,
                ValueDataNode node, IDependencyCollection dependencies, ISerializationContext? context)
        {
            return new ValidatedValueNode(node);
        }

        public DataNode Write(ISerializationManager serializationManager, EntityUid value,
            IDependencyCollection dependencies, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            if (WritingComponent != "Transform" && (Prototype?.NoSpawn == false))
            {
                // Maybe this will be necessary in the future, but at the moment it just indicates that there is some
                // issue, like a non-nullable entityUid data-field. If a component MUST have an entity uid to work with,
                // then the prototype very likely has to be a no-spawn entity that is never meant to be directly spawned.
                Assert.Fail($"Uninitialized entities should not be saving entity Uids. Component: {WritingComponent}. Prototype: {Prototype.ID}");
            }

            return new ValueDataNode(value.Id.ToString());
        }

        EntityUid ITypeReader<EntityUid, ValueDataNode>.Read(ISerializationManager serializationManager,
            ValueDataNode node,
            IDependencyCollection dependencies,
            SerializationHookContext hookCtx,
            ISerializationContext? context, ISerializationManager.InstantiationDelegate<EntityUid>? instanceProvider)
        {
            return EntityUid.Parse(node.Value, "0");
        }
    }
}
