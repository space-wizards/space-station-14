using Content.IntegrationTests.Tests.Interaction;
using Content.Server.Materials;
using Content.Shared.Materials;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;
using Content.IntegrationTests.Utility;
using Robust.Shared.Prototypes;
using NetCord;
using System.Linq;

namespace Content.IntegrationTests.Tests.Materials;
//ProtoIDs we may need
// private static readonly EntProtoId DefibrillatorProtoId = "Defibrillator";

/// <summary>
/// Tests to prevent Recycler loops, where the product of one recycling can be recycled again.
/// </summary>
[TestOf(typeof(MaterialReclaimerSystem))]
[TestOf(typeof(MaterialReclaimerComponent))]
public sealed class ReclaimerLoopTest : InteractionTest
{
    private static string[] _materials = GameDataScrounger.EntitiesWithComponent("PhysicalComposition");
    private static string[] _reclaimers = GameDataScrounger.EntitiesWithComponent("MaterialReclaimer");



    /// <summary>
    /// For each entity that recycles into materials, recycle it and check that
    /// </summary>
    [Test, Combinatorial]
    [TestOf(typeof(MaterialReclaimerSystem))]
    [TestOf(typeof(MaterialReclaimerComponent))]
    public async Task RecyclerLoopTest(string reclaimerID)
    {
        var materialReclaimerSystem = SEntMan.System<MaterialReclaimerSystem>();

        //go through all recyclable items, compile a list of produceable materials
        string[] produceableMaterials = [];
        foreach (String itemID in _materials)
        {
            EntityPrototype item = ProtoMan.Index(itemID);
            item.TryGetComponent<PhysicalCompositionComponent>("PhysicalCompositionComponent", out var compositionComp);
            foreach ((var mat, var value) in compositionComp.MaterialComposition) //for each material they spawn
            {
                //If its not already in producedMaterials, add it
                if (!produceableMaterials.Contains(mat))
                {
                    produceableMaterials.Append(mat);
                }
            }
        }

        //For each produceable Material, assert that it is not recyclable (and would thus cause a recycling loop)
        foreach (string material in produceableMaterials)
        {
            await PlaceInHands(material);

            var reclaimerNetEnt = await SpawnTarget(reclaimerID);

            // Power the reclaimer
            await SpawnEntity("APCBasic", SEntMan.GetCoordinates(TargetCoords));
            await RunTicks(1);
        }

    }
}
