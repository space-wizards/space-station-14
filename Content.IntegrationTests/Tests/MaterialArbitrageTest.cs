#nullable enable
using System.Collections.Generic;
using Content.Server.Cargo.Systems;
using Content.Server.Construction.Completions;
using Content.Server.Construction.Components;
using Content.Server.Destructible;
using Content.Server.Destructible.Thresholds.Behaviors;
using Content.Server.Lathe;
using Content.Server.Stack;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Construction.Prototypes;
using Content.Shared.Construction.Steps;
using Content.Shared.FixedPoint;
using Content.Shared.Lathe;
using Content.Shared.Materials;
using Content.Shared.Research.Prototypes;
using Content.Shared.Stacks;
using Content.Shared.Tools.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests;

/// <summary>
/// This test checks that any destructible or constructible entities do not drop more resources than are required to
/// create them.
/// </summary>
[TestFixture]
public sealed class MaterialArbitrageTest
{
    // These recipes are currently broken and need fixing. You should not be adding to these sets.
    private readonly HashSet<string> _destructionArbitrageIgnore =
    [
        "BaseChemistryEmptyVial", "DrinkShotGlass", "Beaker", "SodiumLightTube", "DrinkGlassCoupeShaped",
        "LedLightBulb", "ExteriorLightTube", "LightTube", "DrinkGlass", "DimLightBulb", "LightBulb", "LedLightTube",
        "SheetRGlass1", "ChemistryEmptyBottle01", "WarmLightBulb",
    ];

    private readonly HashSet<string> _compositionArbitrageIgnore =
    [
        "FoodPlateSmall", "AirTank", "FoodPlateTin", "FoodPlateMuffinTin", "WeaponCapacitorRechargerCircuitboard",
        "WeaponCapacitorRechargerCircuitboard", "BorgChargerCircuitboard", "BorgChargerCircuitboard", "FoodPlate",
        "CellRechargerCircuitboard", "CellRechargerCircuitboard",
    ];

    [Test]
    public async Task NoMaterialArbitrage()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var testMap = await pair.CreateTestMap();
        await server.WaitIdleAsync();

        var entManager = server.ResolveDependency<IEntityManager>();
        var protoManager = server.ResolveDependency<IPrototypeManager>();

        var pricing = entManager.System<PricingSystem>();
        var stackSys = entManager.System<StackSystem>();
        var mapSystem = server.System<SharedMapSystem>();
        var latheSys = server.System<LatheSystem>();
        var compFact = server.ResolveDependency<IComponentFactory>();

        Assert.That(mapSystem.IsInitialized(testMap.MapId));

        var constructionName = compFact.GetComponentName(typeof(ConstructionComponent));
        var compositionName = compFact.GetComponentName(typeof(PhysicalCompositionComponent));
        var materialName = compFact.GetComponentName(typeof(MaterialComponent));
        var destructibleName = compFact.GetComponentName(typeof(DestructibleComponent));
        var refinableName = compFact.GetComponentName(typeof(ToolRefinableComponent));

        // get the inverted lathe recipe dictionary
        var latheRecipes = latheSys.InverseRecipes;

        // Find the lowest multiplier / optimal lathe that can be used to construct a recipie.
        var minMultiplier = new Dictionary<ProtoId<LatheRecipePrototype>, float>();

        foreach (var (_, lathe) in pair.GetPrototypesWithComponent<LatheComponent>())
        {
            foreach (var recipe in latheSys.GetAllPossibleRecipes(lathe))
            {
                if (!minMultiplier.TryGetValue(recipe, out var min))
                    min = 1;

                minMultiplier[recipe] = Math.Min(min, lathe.MaterialUseMultiplier);
            }
        }

        // create construction dictionary
        Dictionary<string, ConstructionComponent> constructionRecipes = new();
        foreach (var proto in protoManager.EnumeratePrototypes<EntityPrototype>())
        {
            if (proto.HideSpawnMenu || proto.Abstract || pair.IsTestPrototype(proto))
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
            if (graph.Start == null)
                continue;

            if (!graph.TryPath(graph.Start, comp.Node, out var path) || path.Length == 0)
                continue;

            var cur = graph.Nodes[graph.Start];
            foreach (var node in path)
            {
                var edge = cur.GetEdge(node.Name);
                cur = node;

                if (edge == null)
                    continue;

                foreach (var step in edge.Steps)
                {
                    if (step is not MaterialConstructionGraphStep materialStep)
                        continue;

                    var stackProto = protoManager.Index<StackPrototype>(materialStep.MaterialPrototypeId);
                    var spawnProto = protoManager.Index(stackProto.Spawn);

                    if (!spawnProto.Components.ContainsKey(materialName) ||
                        !spawnProto.Components.TryGetValue(compositionName, out var compositionReg))
                        continue;

                    var mat = (PhysicalCompositionComponent) compositionReg.Component;
                    foreach (var (matId, amount) in mat.MaterialComposition)
                    {
                        materials[matId] = materialStep.Amount * amount + materials.GetValueOrDefault(matId);
                    }
                }
            }
            constructionMaterials.Add(id, materials);
        }

        Dictionary<string, double> priceCache = new();

        Dictionary<string, (Dictionary<string, int> Ents, Dictionary<string, int> Mats)> spawnedOnDestroy = new();

        // cache the compositions of entities
        // If the entity is refineable (i.e. glass shared can be turned into glass, we take the greater of the two compositions.
        Dictionary<EntProtoId, Dictionary<string, int>> compositions = new();
        foreach (var proto in protoManager.EnumeratePrototypes<EntityPrototype>())
        {
            Dictionary<string, int>? baseComposition = null;

            if (proto.Components.ContainsKey(materialName)
                && proto.Components.TryGetValue(compositionName, out var compositionReg))
            {
                var compositionComp = (PhysicalCompositionComponent)compositionReg.Component;
                baseComposition = compositionComp.MaterialComposition;

            }

            if (!proto.Components.TryGetValue(refinableName, out var refinableReg))
            {
                if (baseComposition != null)
                    compositions[proto.ID] = new(baseComposition);
                continue;
            }

            var composition = new Dictionary<string, int>();
            compositions.Add(proto.ID, composition);

            var refinable = (ToolRefinableComponent)refinableReg.Component;
            foreach (var refineResult in refinable.RefineResult)
            {
                if (refineResult.PrototypeId == null)
                    continue;

                var refineProto = protoManager.Index(refineResult.PrototypeId.Value);
                if (!refineProto.Components.ContainsKey(materialName))
                    continue;

                if (!refineProto.Components.TryGetValue(compositionName, out var refinedCompositionReg))
                    continue;

                var refinedCompositionComp = (PhysicalCompositionComponent)refinedCompositionReg.Component;

                // This assumes refine results do not have complex spawn behaviours like exclusive groups.
                var quantity = refineResult.MaxAmount;

                foreach (var (matId, amount) in refinedCompositionComp.MaterialComposition)
                {
                    composition[matId] = quantity * amount + composition.GetValueOrDefault(matId);
                }
            }

            if (baseComposition == null)
                continue;

            // If the un-refined material quantity is greater than the refined quantity, we use that instead.
            foreach (var (matId, amount) in baseComposition)
            {
                composition[matId] = Math.Max(amount, composition.GetValueOrDefault(matId));
            }
        }

        // Here we get the set of entities/materials spawned when destroying an entity.
        foreach (var proto in protoManager.EnumeratePrototypes<EntityPrototype>())
        {
            if (proto.HideSpawnMenu || proto.Abstract || pair.IsTestPrototype(proto))
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

                        if (!compositions.TryGetValue(key, out var composition))
                            continue;

                        foreach (var (matId, amount) in composition)
                        {
                            spawnedMats[matId] = value.Max * amount + spawnedMats.GetValueOrDefault(matId);
                        }
                    }
                }
            }

            if (spawnedEnts.Count > 0)
                spawnedOnDestroy.Add(proto.ID, (spawnedEnts, spawnedMats));
        }

        // This is the main loop where we actually check for destruction arbitrage
        await Assert.MultipleAsync(async () =>
        {
            foreach (var (id, (spawnedEnts, spawnedMats)) in spawnedOnDestroy)
            {
                if (_destructionArbitrageIgnore.Contains(id))
                    continue;

                // Check cargo sell price
                // several constructible entities have no sell price
                // also this test only really matters if the entity is also purchaseable.... eh..
                var spawnedPrice = await GetSpawnedPrice(spawnedEnts);
                var price = await GetPrice(id);
                if (spawnedPrice > 0 && price > 0)
                    Assert.That(spawnedPrice, Is.LessThanOrEqualTo(price), $"{id} increases in price after being destroyed\nEntities spawned on destruction: {string.Join(',', spawnedEnts)}");

                // Check lathe production
                if (latheRecipes.TryGetValue(id, out var recipes))
                {
                    foreach (var recipe in recipes)
                    {
                        if (!minMultiplier.TryGetValue(recipe, out var multiplier))
                        {
                            server.Log.Info($"Unused lathe recipe? {recipe.ID}?");
                            continue;
                        }
                        foreach (var (matId, amount) in recipe.Materials)
                        {
                            var actualAmount = SharedLatheSystem.AdjustMaterial(amount, recipe.ApplyMaterialDiscount, multiplier);
                            if (spawnedMats.TryGetValue(matId, out var numSpawned))
                                Assert.That(numSpawned, Is.LessThanOrEqualTo(actualAmount), $"destroying a {id} spawns more {matId} than required to produce via an (upgraded) lathe.");
                        }
                    }
                }

                // Check construction.
                if (constructionMaterials.TryGetValue(id, out var constructionMats))
                {
                    foreach (var (matId, amount) in constructionMats)
                    {
                        if (spawnedMats.TryGetValue(matId, out var numSpawned))
                            Assert.That(numSpawned, Is.LessThanOrEqualTo(amount), $"destroying a {id} spawns more {matId} than required to construct it.");
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

                if (edge == null)
                    continue;

                foreach (var completion in edge.Completed)
                {
                    if (completion is not SpawnPrototype spawnCompletion)
                        continue;

                    var spawnProto = protoManager.Index<EntityPrototype>(spawnCompletion.Prototype);

                    if (!spawnProto.Components.ContainsKey(materialName) ||
                        !spawnProto.Components.TryGetValue(compositionName, out var compositionReg))
                        continue;

                    var mat = (PhysicalCompositionComponent) compositionReg.Component;
                    foreach (var (matId, amount) in mat.MaterialComposition)
                    {
                        materials[matId] = spawnCompletion.Amount * amount + materials.GetValueOrDefault(matId);
                    }
                }
            }
            deconstructionMaterials.Add(id, materials);
        }

        // This is functionally the same loop as before, but now testing deconstruction rather than destruction.
        // This is pretty brain-dead. In principle construction graphs can have loops and whatnot.

        await Assert.MultipleAsync(async () =>
        {
            foreach (var (id, deconstructedMats) in deconstructionMaterials)
            {
                // Check cargo sell price
                var deconstructedPrice = await GetDeconstructedPrice(deconstructedMats);
                var price = await GetPrice(id);
                if (deconstructedPrice > 0 && price > 0)
                    Assert.That(deconstructedPrice, Is.LessThanOrEqualTo(price), $"{id} increases in price after being deconstructed");

                // Check lathe production
                if (latheRecipes.TryGetValue(id, out var recipes))
                {
                    foreach (var recipe in recipes)
                    {
                        if (!minMultiplier.TryGetValue(recipe, out var multiplier))
                        {
                            server.Log.Info($"Unused lathe recipe? {recipe.ID}?");
                            continue;
                        }
                        foreach (var (matId, amount) in recipe.Materials)
                        {
                            var actualAmount = SharedLatheSystem.AdjustMaterial(amount, recipe.ApplyMaterialDiscount, multiplier);
                            if (deconstructedMats.TryGetValue(matId, out var numSpawned))
                                Assert.That(numSpawned, Is.LessThanOrEqualTo(actualAmount), $"deconstructing {id} spawns more {matId} than required to produce via an (upgraded) lathe.");
                        }
                    }
                }

                // Check construction.
                if (constructionMaterials.TryGetValue(id, out var constructionMats))
                {
                    foreach (var (matId, amount) in constructionMats)
                    {
                        if (deconstructedMats.TryGetValue(matId, out var numSpawned))
                            Assert.That(numSpawned, Is.LessThanOrEqualTo(amount), $"deconstructing a {id} spawns more {matId} than required to construct it.");
                    }
                }
            }
        });

        // create physical composition dictionary
        // this doesn't account for the chemicals in the composition
        Dictionary<string, PhysicalCompositionComponent> physicalCompositions = new();
        foreach (var proto in protoManager.EnumeratePrototypes<EntityPrototype>())
        {
            if (proto.HideSpawnMenu || proto.Abstract || pair.IsTestPrototype(proto))
                continue;

            if (!proto.Components.TryGetValue(compositionName, out var composition))
                continue;

            var comp = (PhysicalCompositionComponent) composition.Component;
            physicalCompositions.Add(proto.ID, comp);
        }

        // This is functionally the same loop as before, but now testing composition rather than destruction or deconstruction.
        // This doesn't take into account chemicals generated when deconstructing. Maybe it should.
        await Assert.MultipleAsync(async () =>
        {
            foreach (var (id, compositionComponent) in physicalCompositions)
            {
                if (_compositionArbitrageIgnore.Contains(id))
                    continue;

                // Check cargo sell price
                var materialPrice = await GetDeconstructedPrice(compositionComponent.MaterialComposition);
                var chemicalPrice = await GetChemicalCompositionPrice(compositionComponent.ChemicalComposition);
                var sumPrice = materialPrice + chemicalPrice;
                var price = await GetPrice(id);
                if (sumPrice > 0 && price > 0)
                    Assert.That(sumPrice, Is.LessThanOrEqualTo(price), $"{id} increases in price after decomposed into raw materials");

                // Check lathe production
                if (latheRecipes.TryGetValue(id, out var recipes))
                {
                    foreach (var recipe in recipes)
                    {
                        if (!minMultiplier.TryGetValue(recipe, out var multiplier))
                        {
                            server.Log.Info($"Unused lathe recipe? {recipe.ID}?");
                            continue;
                        }
                        foreach (var (matId, amount) in recipe.Materials)
                        {
                            var actualAmount = SharedLatheSystem.AdjustMaterial(amount, recipe.ApplyMaterialDiscount, multiplier);
                            if (compositionComponent.MaterialComposition.TryGetValue(matId, out var numSpawned))
                                Assert.That(numSpawned, Is.LessThanOrEqualTo(actualAmount), $"The physical composition of {id} has more {matId} than required to produce via an (upgraded) lathe.");
                        }
                    }
                }

                // Check construction.
                if (constructionMaterials.TryGetValue(id, out var constructionMats))
                {
                    foreach (var (matId, amount) in constructionMats)
                    {
                        if (compositionComponent.MaterialComposition.TryGetValue(matId, out var numSpawned))
                            Assert.That(numSpawned, Is.LessThanOrEqualTo(amount), $"The physical composition of {id} has more {matId} than required to construct it.");
                    }
                }
            }
        });

        await server.WaitPost(() => mapSystem.DeleteMap(testMap.MapId));
        await pair.CleanReturnAsync();

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
                    priceCache[id] = price = pricing.GetPrice(ent, false);
                    entManager.DeleteEntity(ent);
                });
            }
            return price;
        }

#pragma warning disable CS1998
        async Task<double> GetDeconstructedPrice(Dictionary<string, int> mats)
        {
            double price = 0;
            foreach (var (id, num) in mats)
            {
                var matProto = protoManager.Index<MaterialPrototype>(id);
                price += num * matProto.Price;
            }
            return price;
        }
#pragma warning restore CS1998

#pragma warning disable CS1998
        async Task<double> GetChemicalCompositionPrice(Dictionary<string, FixedPoint2> mats)
        {
            double price = 0;
            foreach (var (id, num) in mats)
            {
                var reagentProto = protoManager.Index<ReagentPrototype>(id);
                price += num.Double() * reagentProto.PricePerUnit;
            }
            return price;
        }
#pragma warning restore CS1998
    }
}
