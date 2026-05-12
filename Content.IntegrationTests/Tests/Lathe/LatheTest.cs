#nullable enable
using System.Collections.Generic;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.Utility;
using Content.Shared.Lathe;
using Content.Shared.Materials;
using Content.Shared.Research.Prototypes;
using Content.Shared.Whitelist;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Lathe;

public sealed class LatheTest : GameTest
{
    [SidedDependency(Side.Server)] private IComponentFactory _sCompFactory = null!;
    [SidedDependency(Side.Server)] private EntityWhitelistSystem _sWhitelistSystem = null!;
    [SidedDependency(Side.Server)] private SharedLatheSystem _sLatheSystem = null!;

    private static readonly string[] LatheProtos = GameDataScrounger.EntitiesWithComponent("Lathe");
    private static readonly string[] MaterialEntityProtos = GameDataScrounger.EntitiesWithComponent("PhysicalComposition");
    private static readonly string[] LatheRecipes = GameDataScrounger.PrototypesOfKind<LatheRecipePrototype>();

    [Test]
    [Description("Checks that lathes can accept ingredients containing the materials required for their recipes.")]
    public async Task TestLatheRecipeIngredientsFitLathe()
    {
        await Server.WaitAssertion(() =>
        {
            // Spawn all of the above material EntityPrototypes - we need actual entities to do whitelist checks
            var materialEntities = new List<EntityUid>(MaterialEntityProtos.Length);
            foreach (var materialEntityProto in MaterialEntityProtos)
            {
                materialEntities.Add(SSpawn(materialEntityProto));
            }

            using (Assert.EnterMultipleScope())
            {
                // Check each lathe individually
                foreach (var latheProtoId in LatheProtos)
                {
                    var latheProto = SProtoMan.Index(latheProtoId);

                    Assert.That(latheProto.TryGetComponent<LatheComponent>(out var latheComp, _sCompFactory));
                    Assert.That(latheProto.TryGetComponent<MaterialStorageComponent>(out var storageComp, _sCompFactory));

                    // Test which material-containing entities are accepted by this lathe
                    var acceptedMaterials = new HashSet<ProtoId<MaterialPrototype>>();
                    foreach (var materialEntity in materialEntities)
                    {
                        var compositionComponent = SComp<PhysicalCompositionComponent>(materialEntity);
                        if (_sWhitelistSystem.IsWhitelistFail(storageComp!.Whitelist, materialEntity))
                            continue;

                        // Mark the lathe as accepting each material in the entity
                        foreach (var (material, _) in compositionComponent.MaterialComposition)
                        {
                            acceptedMaterials.Add(material);
                        }
                    }

                    // Collect all possible recipes assigned to this lathe
                    var recipes = new HashSet<ProtoId<LatheRecipePrototype>>();
                    _sLatheSystem.AddRecipesFromPacks(recipes, latheComp!.StaticPacks);
                    _sLatheSystem.AddRecipesFromPacks(recipes, latheComp.DynamicPacks);
                    if (latheProto.TryGetComponent<EmagLatheRecipesComponent>(out var emagRecipesComp, _sCompFactory))
                    {
                        _sLatheSystem.AddRecipesFromPacks(recipes, emagRecipesComp.EmagStaticPacks);
                        _sLatheSystem.AddRecipesFromPacks(recipes, emagRecipesComp.EmagDynamicPacks);
                    }

                    // Check each recipe assigned to this lathe
                    foreach (var recipeId in recipes)
                    {
                        if (!SProtoMan.TryIndex(recipeId, out var recipeProto))
                        {
                            Assert.Fail($"Lathe recipe '{recipeId}' does not exist");
                            continue;
                        }

                        // Track the total material volume of the recipe
                        var totalQuantity = 0;
                        // Check each material called for by the recipe
                        foreach (var (materialId, quantity) in recipeProto.Materials)
                        {
                            Assert.That(SProtoMan.HasIndex(materialId), $"Material '{materialId}' does not exist");
                            // Make sure the material is accepted by the lathe
                            Assert.That(acceptedMaterials, Does.Contain(materialId), $"Lathe {latheProto.ID} has recipe {recipeId} but does not accept any materials containing {materialId}");
                            totalQuantity += quantity;
                        }
                        // Make sure the recipe doesn't call for more material than the lathe can hold
                        if (storageComp!.StorageLimit != null)
                            Assert.That(totalQuantity, Is.LessThanOrEqualTo(storageComp.StorageLimit), $"Lathe {latheProto.ID} has recipe {recipeId} which calls for {totalQuantity} units of materials but can only hold {storageComp.StorageLimit}");
                    }
                }
            }
        });
    }

    [TestCaseSource(nameof(LatheRecipes))]
    [Description($"Checks that all recipes produce either a {nameof(LatheRecipePrototype.Result)} entity or {nameof(LatheRecipePrototype.ResultReagents)}.")]
    public async Task AllLatheRecipesValidTest(string recipeProtoId)
    {
        var recipe = SProtoMan.Index<LatheRecipePrototype>(recipeProtoId);
        if (recipe.Result == null)
            Assert.That(recipe.ResultReagents, Is.Not.Null, $"Recipe '{recipe.ID}' has no result or result reagents.");
    }
}
