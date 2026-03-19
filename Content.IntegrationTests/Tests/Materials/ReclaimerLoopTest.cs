using System.Collections.Generic;
using Content.IntegrationTests.Tests.Interaction;
using Content.IntegrationTests.Utility;
using Content.Server.Materials;
using Content.Shared.Whitelist;
using Content.Shared.Materials;
using Robust.Shared.Prototypes;
using Robust.Shared.GameObjects;
using Content.Server.Spawners.Components;






namespace Content.IntegrationTests.Tests.Materials;


/// <summary>
/// Tests to prevent Recycler loops, where the product of one recycling can be recycled again.
/// </summary>
[TestOf(typeof(MaterialReclaimerSystem))]
[TestOf(typeof(MaterialReclaimerComponent))]
public sealed class ReclaimerLoopTest : InteractionTest
{
    //ProtoIDs we need
    private static readonly EntProtoId APCPid = "APCBasic";
    private static readonly EntProtoId FloorTileID = "FloorTileItemSteelCheckerDark";

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
        var materialReclaimerSystem = SEntMan.System<SharedMaterialReclaimerSystem>();
        var entityWhitelistSystem = SEntMan.System<EntityWhitelistSystem>();

        await AddAtmosphere(); //so the player can breathe

        //Spawn the reclaimer
        await SpawnTarget(reclaimerID, PlayerCoords);
        Assert.That(STarget, Is.Not.Null,
            "STarget was null, did the reclaimer spawn correctly?");
        var reclaimComp = Comp<MaterialReclaimerComponent>(Target);
        //If the reclaimer can produce materials
        var reclaimsMaterials = reclaimComp.ReclaimMaterials;

        //if the reclaimer has reclaimsMaterials and is able to produce materials
        //go through all recyclable items, compile a HashSet of produceable materials
        HashSet<string> produceableMaterials = new HashSet<string>(); //If reclaimMaterials is false, this will stay empty
        if (reclaimsMaterials)
        {
            foreach (string itemID in GameDataScrounger.EntitiesWithComponent("PhysicalComposition"))
            {
                //spawners mess the system up something fierce
                if (HasComp<EntityTableSpawnerComponent>())
                    continue;

                EntityPrototype item = ProtoMan.Index(itemID);
                var currentScrap = await Spawn(itemID);
                var currentScrapUid = SEntMan.GetEntity(currentScrap);
                var currentScrapCompositionComp = Comp<PhysicalCompositionComponent>(currentScrap);

                //If it's on the whitelist for the reclaimer, and not on its blacklist.
                if (entityWhitelistSystem.CheckBoth(currentScrapUid, reclaimComp.Blacklist, reclaimComp.Whitelist))
                {
                    //for each material it produces, add it to the HashSet
                    foreach ((var mat, var value) in currentScrapCompositionComp.MaterialComposition)
                    {
                        ProtoId<MaterialPrototype> matAsProto = ProtoMan.Index<MaterialPrototype>(mat);
                        produceableMaterials.Add(matAsProto);
                    }
                }
                await Delete(currentScrapUid);
            }
        }

        // Power the reclaimer
        await SpawnEntity(APCPid, SEntMan.GetCoordinates(TargetCoords));
        await RunTicks(1);

        //Set reclaimer to enabled
        materialReclaimerSystem.SetReclaimerEnabled((EntityUid)STarget, true);
        //Assert that reclaimer enabled
        Assert.That(reclaimComp.Enabled,
        "The reclaimer did not get or stay enabled");

        // //put a floor tile down
        await InteractUsing(FloorTileID);

        //For each produceable Material, assert that it is not recyclable (and would thus cause a recycling loop)
        foreach (ProtoId<MaterialPrototype> material in produceableMaterials)
        {
            EntProtoId? matStack = ProtoMan.Index(material).StackEntity;
            Assert.That(matStack, Is.Not.Null,
            $"The material, {material}, did not have a stackentity associated with it. You may need to add a stackEntity to its Reagents/Materials yml file.");

            var matInHands = await PlaceInHands(matStack);
            var matInHandsUid = SEntMan.GetEntity(matInHands);

            //Assert we're holding material
            Assert.That(HandSys.GetActiveItem((SPlayer, Hands)), Is.EqualTo(matInHandsUid),
            $"The material, {matStack}, never got put in our hands.");

            await Interact();

            //Assert Hands not empty
            Assert.That(HandSys.GetActiveItem((ToServer(Player), Hands)), Is.Not.EqualTo(null),
            $"The material that should not have been reclaimed, {matStack}, is no longer in our hands. The reclaimer was {reclaimerID}");
        }
    }
}
