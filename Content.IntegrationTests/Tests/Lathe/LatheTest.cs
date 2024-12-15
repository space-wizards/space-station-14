using System.Collections.Generic;
using System.Linq;
using Content.Shared.Lathe;
using Content.Shared.Materials;
using Content.Shared.Prototypes;
using Content.Shared.Whitelist;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Lathe;

[TestFixture]
public sealed class LatheTest
{
    [Test]
    public async Task TestLatheRecipeIngredientsFitLathe()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var mapData = await pair.CreateTestMap();

        var entMan = server.EntMan;
        var protoMan = server.ProtoMan;
        var compFactory = server.ResolveDependency<IComponentFactory>();
        var materialStorageSystem = server.System<SharedMaterialStorageSystem>();
        var whitelistSystem = server.System<EntityWhitelistSystem>();

        await server.WaitAssertion(() =>
        {
            // Find all the lathes
            var latheProtos = protoMan.EnumeratePrototypes<EntityPrototype>()
                .Where(p => !p.Abstract)
                .Where(p => !pair.IsTestPrototype(p))
                .Where(p => p.HasComponent<LatheComponent>());

            // Find every EntityPrototype that can be inserted into a MaterialStorage
            var materialEntityProtos = protoMan.EnumeratePrototypes<EntityPrototype>()
                .Where(p => !p.Abstract)
                .Where(p => !pair.IsTestPrototype(p))
                .Where(p => p.HasComponent<PhysicalCompositionComponent>());

            // Spawn all of the above material EntityPrototypes - we need actual entities to do whitelist checks
            var materialEntities = new List<EntityUid>(materialEntityProtos.Count());
            foreach (var materialEntityProto in materialEntityProtos)
            {
                materialEntities.Add(entMan.SpawnEntity(materialEntityProto.ID, mapData.GridCoords));
            }

            Assert.Multiple(() =>
            {
                // Check each lathe individually
                foreach (var latheProto in latheProtos)
                {
                    if (!latheProto.TryGetComponent<LatheComponent>(out var latheComp, compFactory))
                        continue;

                    if (!latheProto.TryGetComponent<MaterialStorageComponent>(out var storageComp, compFactory))
                        continue;

                    // Test which material-containing entities are accepted by this lathe
                    var acceptedMaterials = new HashSet<ProtoId<MaterialPrototype>>();
                    foreach (var materialEntity in materialEntities)
                    {
                        Assert.That(entMan.TryGetComponent<PhysicalCompositionComponent>(materialEntity, out var compositionComponent));
                        if (whitelistSystem.IsWhitelistFail(storageComp.Whitelist, materialEntity))
                            continue;

                        // Mark the lathe as accepting each material in the entity
                        foreach (var (material, _) in compositionComponent.MaterialComposition)
                        {
                            acceptedMaterials.Add(material);
                        }
                    }

                    // Check each recipe assigned to this lathe
                    foreach (var recipeId in latheComp.StaticRecipes)
                    {
                        Assert.That(protoMan.TryIndex(recipeId, out var recipeProto));

                        // Check each material called for by the recipe
                        foreach (var (materialId, _) in recipeProto.Materials)
                        {
                            Assert.That(protoMan.TryIndex(materialId, out var materialProto));
                            // Make sure the material is accepted by the lathe
                            Assert.That(acceptedMaterials, Does.Contain(materialId), $"Lathe {latheProto.ID} has recipe {recipeId} but does not accept any materials containing {materialId}");
                        }
                    }
                }
            });
        });

        await pair.CleanReturnAsync();
    }
}
