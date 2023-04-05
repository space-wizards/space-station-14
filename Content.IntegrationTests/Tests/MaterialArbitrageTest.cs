using Content.Server.Cargo.Systems;
using Content.Server.Construction.Completions;
using Content.Server.Construction.Components;
using Content.Server.Destructible;
using Content.Server.Destructible.Thresholds.Behaviors;
using Content.Server.Stack;
using Content.Shared.Construction.Prototypes;
using Content.Shared.Construction.Steps;
using Content.Shared.Lathe;
using Content.Shared.Research.Prototypes;
using Content.Shared.Stacks;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Shared.Construction.Components;

namespace Content.IntegrationTests.Tests;

/// <summary>
/// This test checks that any destructible or constructible entities do not drop more resources than are required to
/// create them.
/// </summary>
[TestFixture]
public sealed class MaterialArbitrageTest
{
    [Test]
    public async Task NoMaterialArbitrage()
    {
        // TODO check lathe resource prices?
        // I CBF doing that atm because I know that will probably fail for most lathe recipies.

        await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings() {NoClient = true});
        var server = pairTracker.Pair.Server;

        var testMap = await PoolManager.CreateTestMap(pairTracker);
        await server.WaitIdleAsync();

        var entManager = server.ResolveDependency<IEntityManager>();
        var sysManager = server.ResolveDependency<IEntitySystemManager>();
        var mapManager = server.ResolveDependency<IMapManager>();
        Assert.That(mapManager.IsMapInitialized(testMap.MapId));

        var protoManager = server.ResolveDependency<IPrototypeManager>();
        var pricing = sysManager.GetEntitySystem<PricingSystem>();
        var stackSys = sysManager.GetEntitySystem<StackSystem>();
        var compFact = server.ResolveDependency<IComponentFactory>();

        var constructionName = compFact.GetComponentName(typeof(ConstructionComponent));
        var destructibleName = compFact.GetComponentName(typeof(DestructibleComponent));
        var stackName = compFact.GetComponentName(typeof(StackComponent));

        // construct inverted lathe recipe dictionary
        Dictionary<string, LatheRecipePrototype> latheRecipes = new();
        foreach (var proto in protoManager.EnumeratePrototypes<LatheRecipePrototype>())
        {
            latheRecipes.Add(proto.Result, proto);
        }

        // Lets assume the possible lathe for resource multipliers:
        var multiplier = MathF.Pow(LatheComponent.DefaultPartRatingMaterialUseMultiplier, MachinePartComponent.MaxRating - 1);

        // create construction dictionary
        Dictionary<string, ConstructionComponent> constructionRecipes = new();
        foreach (var proto in protoManager.EnumeratePrototypes<EntityPrototype>())
        {
            if (proto.NoSpawn || proto.Abstract)
                continue;

            if (!proto.Components.TryGetValue(constructionName, out var destructible))
                continue;

            var comp = (ConstructionComponent) destructible.Component;
            constructionRecipes.Add(proto.ID, comp);
        }

        // Get ingredients required to construct an entity
        Dictionary<string, Dictionary<string, int>> constructionMaterials = new();
        foreach (var (id, comp) in constructionRecipes)
        {
            var materials = new Dictionary<string, int>();
            var graph = protoManager.Index<ConstructionGraphPrototype>(comp.Graph);
            if (!graph.TryPath(graph.Start, comp.Node, out var path) || path.Length == 0)
                continue;

            var cur = graph.Nodes[graph.Start];
            foreach (var node in path)
            {
                var edge = cur.GetEdge(node.Name);
                cur = node;

                foreach (var step in edge.Steps)
                {
                    if (step is MaterialConstructionGraphStep materialStep)
                        materials[materialStep.MaterialPrototypeId] = materialStep.Amount + materials.GetValueOrDefault(materialStep.MaterialPrototypeId);
                }
            }
            constructionMaterials.Add(id, materials);
        }

        Dictionary<string, double> priceCache = new();

        Dictionary<string, (Dictionary<string, int> Ents, Dictionary<string, int> Mats)> spawnedOnDestroy = new();

        // Here we get the set of entities/materials spawned when destroying an entity.
        foreach (var proto in protoManager.EnumeratePrototypes<EntityPrototype>())
        {
            if (proto.NoSpawn || proto.Abstract)
                continue;

            if (!proto.Components.TryGetValue(destructibleName, out var destructible))
                continue;

            var comp = (DestructibleComponent) destructible.Component;

            var spawnedEnts = new Dictionary<string, int>();
            var spawnedMats = new Dictionary<string, int>();

            // This test just blindly assumes that ALL spawn entity behaviors get triggered. In reality, some entities
            // might only trigger a subset. If that starts being a problem, this test either needs fixing or needs to
            // get an ignored prototypes list.

            foreach (var threshold in comp.Thresholds)
            {
                foreach (var behaviour in threshold.Behaviors)
                {
                    if (behaviour is not SpawnEntitiesBehavior spawn)
                        continue;

                    foreach (var (key, value) in spawn.Spawn)
                    {
                        spawnedEnts[key] = spawnedEnts.GetValueOrDefault(key) + value.Max;

                        var spawnProto = protoManager.Index<EntityPrototype>(key);
                        if (!spawnProto.Components.TryGetValue(stackName, out var reg))
                            continue;

                        var stack = (StackComponent) reg.Component;
                        spawnedMats[stack.StackTypeId] = value.Max + spawnedMats.GetValueOrDefault(stack.StackTypeId);
                    }
                }
            }

            if (spawnedEnts.Count > 0)
                spawnedOnDestroy.Add(proto.ID, (spawnedEnts, spawnedMats));
        }

        // This is the main loop where we actually check for destruction arbitrage
        Assert.Multiple(async () =>
        {
            foreach (var (id, (spawnedEnts, spawnedMats)) in spawnedOnDestroy)
            {
                // Check cargo sell price
                // several constructible entities have no sell price
                // also this test only really matters if the entity is also purchaseable.... eh..
                var spawnedPrice = await GetSpawnedPrice(spawnedEnts);
                var price = await GetPrice(id);
                if (spawnedPrice > 0 && price > 0)
                    Assert.LessOrEqual(spawnedPrice, price, $"{id} increases in price after being destroyed");

                // Check lathe production
                if (latheRecipes.TryGetValue(id, out var recipe))
                {
                    foreach (var (matId, amount) in recipe.RequiredMaterials)
                    {
                        var actualAmount = SharedLatheSystem.AdjustMaterial(amount, recipe.ApplyMaterialDiscount, multiplier);
                        if (spawnedMats.TryGetValue(matId, out var numSpawned))
                            Assert.LessOrEqual(numSpawned, actualAmount, $"destroying a {id} spawns more {matId} than required to produce via an (upgraded) lathe.");
                    }
                }

                // Check construction.
                if (constructionMaterials.TryGetValue(id, out var constructionMats))
                {
                    foreach (var (matId, amount) in constructionMats)
                    {
                        if (spawnedMats.TryGetValue(matId, out var numSpawned))
                            Assert.LessOrEqual(numSpawned, amount, $"destroying a {id} spawns more {matId} than required to construct it.");
                    }
                }
            }
        });

        // Finally, lets also check for deconstruction arbitrage.
        // Get ingredients returned when deconstructing an entity
        Dictionary<string, Dictionary<string, int>> deconstructionMaterials = new();
        foreach (var (id, comp) in constructionRecipes)
        {
            if (comp.DeconstructionNode == null)
                continue;

            var materials = new Dictionary<string, int>();
            var graph = protoManager.Index<ConstructionGraphPrototype>(comp.Graph);

            if (!graph.TryPath(comp.Node, comp.DeconstructionNode, out var path) || path.Length == 0)
                continue;

            var cur = graph.Nodes[comp.Node];
            foreach (var node in path)
            {
                var edge = cur.GetEdge(node.Name);
                cur = node;

                foreach (var completion in edge.Completed)
                {
                    if (completion is not SpawnPrototype spawnCompletion)
                        continue;

                    var spawnProto = protoManager.Index<EntityPrototype>(spawnCompletion.Prototype);
                    if (!spawnProto.Components.TryGetValue(stackName, out var reg))
                        continue;

                    var stack = (StackComponent) reg.Component;

                    materials[stack.StackTypeId] = spawnCompletion.Amount + materials.GetValueOrDefault(stack.StackTypeId);
                }
            }
            deconstructionMaterials.Add(id, materials);
        }

        // This is functionally the same loop as before, but now testinng deconstruction rather than destruction.
        // This is pretty braindead. In principle construction graphs can have loops and whatnot.

        Assert.Multiple(async () =>
        {
            foreach (var (id, deconstructedMats) in deconstructionMaterials)
            {
                // Check cargo sell price
                var deconstructedPrice = await GetDeconstructedPrice(deconstructedMats);
                var price = await GetPrice(id);
                if (deconstructedPrice > 0 && price > 0)
                    Assert.LessOrEqual(deconstructedPrice, price, $"{id} increases in price after being deconstructed");

                // Check lathe production
                if (latheRecipes.TryGetValue(id, out var recipe))
                {
                    foreach (var (matId, amount) in recipe.RequiredMaterials)
                    {
                        var actualAmount = SharedLatheSystem.AdjustMaterial(amount, recipe.ApplyMaterialDiscount, multiplier);
                        if (deconstructedMats.TryGetValue(matId, out var numSpawned))
                            Assert.LessOrEqual(numSpawned, actualAmount, $"deconstructing {id} spawns more {matId} than required to produce via an (upgraded) lathe.");
                    }
                }

                // Check construction.
                if (constructionMaterials.TryGetValue(id, out var constructionMats))
                {
                    foreach (var (matId, amount) in constructionMats)
                    {
                        if (deconstructedMats.TryGetValue(matId, out var numSpawned))
                            Assert.LessOrEqual(numSpawned, amount, $"deconstructing a {id} spawns more {matId} than required to construct it.");
                    }
                }
            }
        });

        await server.WaitPost(() => mapManager.DeleteMap(testMap.MapId));
        await pairTracker.CleanReturnAsync();

        async Task<double> GetSpawnedPrice(Dictionary<string, int> ents)
        {
            double price = 0;
            foreach (var (id, num) in ents)
            {
                price += num * await GetPrice(id);
            }

            return price;
        }

        async Task<double> GetPrice(string id)
        {
            if (!priceCache.TryGetValue(id, out var price))
            {
                await server.WaitPost(() =>
                {
                    var ent = entManager.SpawnEntity(id, testMap.GridCoords);
                    stackSys.SetCount(ent, 1);
                    priceCache[id] = price = pricing.GetPrice(ent);
                    entManager.DeleteEntity(ent);
                });
            }
            return price;
        }


        async Task<double> GetDeconstructedPrice(Dictionary<string, int> mats)
        {
            double price = 0;
            foreach (var (id, num) in mats)
            {
                var matProto = protoManager.Index<StackPrototype>(id).Spawn;
                price += num * await GetPrice(matProto);
            }
            return price;
        }
    }
}
