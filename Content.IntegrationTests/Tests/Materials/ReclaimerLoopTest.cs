using System.Collections.Generic;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.Tests.Interaction;
using Content.IntegrationTests.Utility;
using Content.Server.Materials;
using Content.Server.Spawners.Components;
using Content.Shared.Materials;
using Content.Shared.Sprite;
using Content.Shared.Whitelist;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Materials;

/// <summary>
/// Tests to prevent Recycler loops, where the product of one recycling can be recycled again.
/// </summary>
[TestOf(typeof(MaterialReclaimerSystem))]
[TestOf(typeof(MaterialReclaimerComponent))]
public sealed class ReclaimerLoopTest : InteractionTest
{
    //ProtoIDs we need
    private static readonly EntProtoId ApcId = "APCBasic";
    private static readonly EntProtoId FloorTileId = "FloorTileItemSteelCheckerDark";

    private static readonly string[] Reclaimers = GameDataScrounger.EntitiesWithComponent("MaterialReclaimer");

    [Test]
    [TestCaseSource(nameof(Reclaimers))]
    [TestOf(typeof(MaterialReclaimerSystem))]
    [TestOf(typeof(MaterialReclaimerComponent))]
    [Description("For every material that a reclaimer can spawn, make sure that it cannot get stuck in a loop of spawning then recycling.")]
    [TrackingIssue("https://github.com/space-wizards/space-station-14/issues/39691")]
    public async Task MaterialSpawnLoopTest(string reclaimerId)
    {
        var materialReclaimerSystem = SEntMan.System<SharedMaterialReclaimerSystem>();
        var entityWhitelistSystem = SEntMan.System<EntityWhitelistSystem>();
        var sCompFactory = Server.Resolve<IComponentFactory>();

        //Spawn the reclaimer
        await SpawnTarget(reclaimerId, PlayerCoords);
        Assert.That(STarget, Is.Not.Null,
            "STarget was null, did the reclaimer spawn correctly?");

        var reclaimComp = Comp<MaterialReclaimerComponent>(Target);
        //If the reclaimer can produce materials
        var reclaimsMaterials = reclaimComp.ReclaimMaterials;

        //if the reclaimer has reclaimsMaterials and is able to produce materials
        //go through all recyclable items, compile a HashSet of produceable materials
        HashSet<ProtoId<MaterialPrototype>> producibleMaterials = []; //If reclaimMaterials is false, this will stay empty
        if (reclaimsMaterials)
        {
            foreach (var proto in ProtoMan.EnumeratePrototypes<EntityPrototype>())
            {
                //we dont care about items that dont recycle into anything physical
                if (!proto.HasComp<PhysicalCompositionComponent>(sCompFactory))
                    continue;

                //spawners and random items mess things up quickly, avoid them too
                if (proto.HasComp<RandomSpriteComponent>(sCompFactory) || proto.HasComp<EntityTableSpawnerComponent>(sCompFactory))
                    continue;

                var currentScrap = await Spawn(proto.ID);
                var currentScrapUid = ToServer(currentScrap);
                var currentScrapCompositionComp = Comp<PhysicalCompositionComponent>(currentScrap);

                //If it's on the whitelist for the reclaimer, and not on its blacklist.
                if (entityWhitelistSystem.CheckBoth(currentScrapUid, reclaimComp.Blacklist, reclaimComp.Whitelist))
                {
                    //for each material it produces, add it to the HashSet
                    foreach (var (mat, _) in currentScrapCompositionComp.MaterialComposition)
                    {
                        ProtoId<MaterialPrototype> matAsProto = ProtoMan.Index<MaterialPrototype>(mat);
                        producibleMaterials.Add(matAsProto);
                    }
                }

                await Delete(currentScrapUid);
            }
        }

        // Power the reclaimer
        await SpawnEntity(ApcId, SEntMan.GetCoordinates(TargetCoords));
        await RunTicks(1);
        //Set reclaimer to enabled
        await Server.WaitPost(() =>
        {
            materialReclaimerSystem.SetReclaimerEnabled((EntityUid)STarget, true);
        });

        //Assert that reclaimer enabled
        Assert.That(reclaimComp.Enabled,
            "The reclaimer did not get or stay enabled");

        //put a floor tile down
        await InteractUsing(FloorTileId);

        using (Assert.EnterMultipleScope())
        {
            //For each produceable Material, assert that it is not recyclable (and would thus cause a recycling loop)
            foreach (ProtoId<MaterialPrototype> material in producibleMaterials)
            {
                var matStack = ProtoMan.Index(material).StackEntity;
                Assert.That(matStack, Is.Not.Null,
                    $"The material, {material}, did not have a stackentity associated with it. You may need to add a stackEntity to its Reagents/Materials yml file.");

                var matInHands = await PlaceInHands(matStack);
                var matInHandsUid = ToServer(matInHands);

                //Assert we're holding material
                Assert.That(HandSys.GetActiveItem((SPlayer, Hands)), Is.EqualTo(matInHandsUid),
                    $"The material, {matStack}, never got put in our hands.");

                await Interact();

                //Assert Hands not empty
                Assert.That(HandSys.GetActiveItem((SPlayer, Hands)), Is.Not.Null,
                    $"The material that should not have been reclaimed, {matStack}, is no longer in our hands. The reclaimer was {reclaimerId}");
            }
        }
    }
}
