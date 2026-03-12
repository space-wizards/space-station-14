#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
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
using Content.Shared.Stacks;
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
    private static readonly ProtoId<DamageTypePrototype> PoisonDamageTypeId = "Poison";
    private static readonly List<EntProtoId> TopicalsToBeTested = new List<EntProtoId> { BruisePack, Ointment, Gauze }; //please dont add HealingToolbox to this

    /// <summary>
    /// Tests that the correct topical heals the right damage.
    /// </summary>
    [Test]
    public async Task TopicalsInteractionsTest()
    {
        var damageableSystem = SEntMan.System<DamageableSystem>();

        await AddAtmosphere(); // prevent the InteractionTestMob from suffocating

        //For every topical we need to test
        foreach (EntProtoId topicalID in TopicalsToBeTested)
        {
            //Spawn a new urist
            var urist = await SpawnTarget(MobHuman);
            var damageableComp = Comp<DamageableComponent>(urist);

            //Hold the topical stack
            var topical = await PlaceInHands(topicalID);

            var healingComp = Comp<HealingComponent>(topical);
            var stackComp = Comp<StackComponent>(topical);
            var emptyDamageSpecifier = new DamageSpecifier();
            var startStackSize = stackComp.Count;

            //get the damage types this is meant to fix
            DamageSpecifier healsTypes = healingComp.Damage;

            //Damage the Urist 2 topicals worth
            damageableSystem.SetDamage((STarget.Value, damageableComp), -2 * healsTypes);
            Assert.That(damageableSystem.GetPositiveDamage((STarget.Value, damageableComp)) == -2 * healsTypes,
            "The urist did not get damaged the correct amount.");

            //Use topical
            await Interact();
            //Assert that could use?

            //Assert each correct Damage lowered the right amount, or at all
            Assert.That(damageableSystem.GetPositiveDamage((STarget.Value, damageableComp)) == -healsTypes,
            "The correct damage types did not lower, or did not lower the correct amount.");
            //Assert that stack number lowered by 1
            Assert.That(stackComp.Count == startStackSize - 1,
            "The topical stack count did not lower, or did not lower by the correct amount.");

            //Use topical
            await Interact();
            //Assert that all damage gone
            Assert.That(damageableSystem.GetPositiveDamage((STarget.Value, damageableComp)) == emptyDamageSpecifier,
            "Damage was not fully removed after using topical twice.");

            //No Topicals work on poison, lets use 10 of it.
            var poisonSpecifier = new DamageSpecifier(ProtoMan.Index(PoisonDamageTypeId), 10);
            damageableSystem.SetDamage((STarget.Value, damageableComp), poisonSpecifier);

            //Try to use the topical
            await Interact(false);
            //Assert could not use (by checking if there are any doafters)
            Assert.That(!ActiveDoAfters.Any(),
            "Topical use went through despite wrong damage type.");
            //Assert Poison damage is unchanged
            Assert.That(damageableSystem.GetPositiveDamage((STarget.Value, damageableComp)) == poisonSpecifier,
            "The topical healed damage it shouldn't.");

            //Add in more damage types (5 of each brute, 5 Caustic, 5 Poison)

            //Use Bruize Pack once

            //Assert brute damage gone

            //Attempt use Bruize Pack
            //Assert could not use

            //Assert non-brute damage types unchanged

            //Reset Damage

            //Set bruize pack stack size to 2

            //Deal 15 brute to the urist

            //Use once

            //Assert id changed to Brutepack1

            //Use again

            //Assert Hands empty

            //Assert some brute damage remains
        }
    }
}
