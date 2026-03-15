using Content.IntegrationTests.Tests.Interaction;
using Content.Server.Materials;
using Content.Shared.Materials;
using Content.IntegrationTests.Utility;
using Robust.Shared.Prototypes;
using System.Linq;
using Robust.Shared.Utility;
using System.Collections.Generic;


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
    private static string[] _reclaimers = GameDataScrounger.EntitiesWithComponent("MaterialReclaimer");

    /// <summary>
    /// For each entity that recycles into materials, recycle it and check that
    /// </summary>
    [Test]
    [TestCaseSource(nameof(_reclaimers))]
    [TestOf(typeof(MaterialReclaimerSystem))]
    [TestOf(typeof(MaterialReclaimerComponent))]
    public async Task ReclaimingLoopTest(string reclaimerID)
    {
        // var materialReclaimerSystem = SEntMan.System<MaterialReclaimerSystem>();

        //go through all recyclable items, compile a list of produceable materials
        List<string> produceableMaterials = [];
        foreach (string itemID in GameDataScrounger.EntitiesWithComponent("PhysicalComposition"))
        {
            EntityPrototype item = ProtoMan.Index(itemID);
            await Server.WaitAssertion(() =>
            {
                item.TryGetComponent<PhysicalCompositionComponent>(out var compositionComp, Factory);
                foreach ((var mat, var value) in compositionComp.MaterialComposition) //for each material they spawn
                {
                    //If its not already in producedMaterials, add it
                    if (!produceableMaterials.Contains(mat))
                    {
                        produceableMaterials.Add(mat);
                    }
                }
            });
        }

        //For each produceable Material, assert that it is not recyclable (and would thus cause a recycling loop)
        foreach (string material in produceableMaterials)
        {

            //Spawn the reclaimer at the player coords, so we're right above it for the drop
            var reclaimerNetEnt = await SpawnTarget(reclaimerID, PlayerCoords);

            // Power the reclaimer
            await SpawnEntity("APCBasic", SEntMan.GetCoordinates(TargetCoords));
            await RunTicks(1);

            await InteractUsing(material);

            //Assert Hands not empty
            Assert.That(HandSys.GetActiveItem((ToServer(Player), Hands)), Is.Not.EqualTo(null),
            $"The material that should not have been reclaimed, {material}, is no longer in our hands.");
        }

    }
}
