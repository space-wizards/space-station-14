#nullable enable
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Medical;
using Content.Shared.Medical.Healing;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Medical;

/// <summary>
/// Tests for topicals.
/// </summary>
[TestOf(typeof(HealingComponent))]
public sealed class TopicalsTest : InteractionTest
{
    private static readonly EntProtoId MobHuman = "MobHuman";
    private static readonly EntProtoId BruisePack = "Brutepack";
    private static readonly EntProtoId Ointment = "Ointment";
    private static readonly EntProtoId Gauze = "Gauze";
    private static readonly ProtoId<DamageTypePrototype> BluntDamageTypeId = "Slash";
    private static readonly ProtoId<DamageTypePrototype> SlashDamageTypeId = "Blunt";
    private static readonly ProtoId<DamageTypePrototype> PiercingDamageTypeId = "Piercing";

    /// <summary>
    /// Tests that the correct topical heals the right damage.
    /// </summary>
    [Test]
    public async Task TopicalInteractionsTest()
    {
        var damageSystem = SEntMan.System<DamageableSystem>();

        await AddAtmosphere(); // prevent the InteractionTestMob from suffocating

        var urist = await SpawnTarget(MobHuman);
        var damageComp = Comp<DamageableComponent>(urist);

        //Hold a bruise pack
        var bruisePackEnt = await PlaceInHands(BruisePack);
        var healingComp = Comp<HealingComponent>(bruisePackEnt);

        // //get the damage types this is meant to fix
        // var healsTypes = healingComp.DamageContainers;

        //Damage the Urist with 15 of each Brute Damage (Blunt, Pierce, and Slash)

        //Assert Damaged

        //Use Bruise Pack once
        //Assert that Could use

        //Assert each Brute Damage lowered, and lowered equally
        //Assert that stack number lowered by 1

        //Use Bruise Pack until doafters stop

        //Assert that Brute Damage gone
        //Assert that stack number lowered by 2

        //Reset Damage

        //Damage the Urist with Heat

        //Use Bruise Pack
        //Assert could not use
        //Assert Heat damage unchanged

        //Reset Damage

        //Add in more damage types (5 of each brute, 5 Caustic, 5 Poison)

        //Use Bruize Pack once

        //Assert brute damage gone

        //Attempt use Bruize Pack
        //Assert could not use

        //Assert non-brute damage types unchanged

        //Reset Damage

        //Set topical
    }
}
