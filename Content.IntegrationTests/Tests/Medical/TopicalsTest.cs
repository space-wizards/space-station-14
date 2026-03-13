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
using Microsoft.Testing.Platform.Extensions.Messages;
using NUnit.Framework.Constraints;
using Robust.Shared.GameObjects;
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
    private static readonly ProtoId<DamageTypePrototype> HeatDamageTypeId = "Heat";
    private static readonly ProtoId<DamageTypePrototype> RadiationDamageTypeId = "Radiation";
    private static readonly ProtoId<DamageTypePrototype> PoisonDamageTypeId = "Poison";
    private static readonly List<EntProtoId> TopicalsToBeTested = new List<EntProtoId>
    { BruisePack, Ointment, Gauze }; //please dont add HealingToolbox to this
    private static readonly List<ProtoId<DamageTypePrototype>> DamageTypesToBeTested = new List<ProtoId<DamageTypePrototype>>
    {BluntDamageTypeId,SlashDamageTypeId,PiercingDamageTypeId,HeatDamageTypeId,RadiationDamageTypeId,PoisonDamageTypeId};

    /// <summary>
    /// Tests that the correct topical heals the right damage.
    /// </summary>
    [Test]
    public async Task TopicalsInteractionsTest()
    {
        var damageableSystem = SEntMan.System<DamageableSystem>();
        var sharedStackSystem = SEntMan.System<SharedStackSystem>();

        await AddAtmosphere(); // prevent the InteractionTestMob from suffocating

        //For every topical we need to test
        foreach (EntProtoId topicalID in TopicalsToBeTested)
        {
            //Spawn a new urist
            var urist = await SpawnTarget(MobHuman);
            var damageableComp = Comp<DamageableComponent>(urist);

            //Hold the topical stack
            var topical = await PlaceInHands(topicalID, 10);
            var topicalUid = SEntMan.GetEntity(topical);

            var healingComp = Comp<HealingComponent>(topical);
            var stackComp = Comp<StackComponent>(topical);

            //Stop passive healing from messing with the numbers
            var passivehealing = Comp<PassiveDamageComponent>(urist);
            passivehealing.Damage = new();

            var emptyDamageSpecifier = new DamageSpecifier();
            var startStackSize = stackComp.Count;
            Assert.That(startStackSize == 10,
            "Stack size is not 10.");

            //get the damage types this is meant to fix
            DamageSpecifier healsTypes = healingComp.Damage;

            //Damage the Urist 2 topicals worth
            var damageAmount = healsTypes * -2;
            damageableSystem.SetDamage((STarget.Value, damageableComp), damageAmount);
            Assert.That(damageableSystem.GetPositiveDamage((STarget.Value, damageableComp)).Equals(damageAmount),
            "The urist did not get damaged the correct amount.");

            //Use topical just once
            await Interact(false);
            //Wait for a bit after the first doAfter finishes
            await RunSeconds(healingComp.Delay.Seconds + 1);
            //Then Cancel that doAfter, having used 1 topical
            await CancelDoAfters();

            //Assert that stack number lowered by 1
            Assert.That(stackComp.Count == startStackSize - 1,
            "The topical stack count did not lower by one per use.");

            //Assert that each correct Damage type lowered by one topical's worth
            var correctDamage = -healsTypes;
            Assert.That(damageableSystem.GetPositiveDamage((STarget.Value, damageableComp)).Equals(correctDamage),
            "The correct damage types did not lower, or did not lower the correct amount.");

            //Use topical
            await Interact();
            //Assert that all damage gone
            Assert.That(damageableSystem.GetPositiveDamage((STarget.Value, damageableComp)).Equals(emptyDamageSpecifier),
            "Damage was not fully removed after using topical twice.");

            //No Topicals work on poison, lets use 10 of it.
            var poisonSpecifier = new DamageSpecifier(ProtoMan.Index(PoisonDamageTypeId), 10);
            damageableSystem.SetDamage((STarget.Value, damageableComp), poisonSpecifier);

            //Try to use the topical
            await Interact(false);
            //Assert could not use (by checking if there are any doafters)
            Assert.That(!ActiveDoAfters.Any(),
            "Topical use started despite wrong damage type.");
            //Assert Poison damage is unchanged
            Assert.That(damageableSystem.GetPositiveDamage((STarget.Value, damageableComp)).Equals(poisonSpecifier),
            "The topical healed damage it shouldn't.");

            //Testing Multiple Damage Types (10 of each specififed in DamageTypesToBeTested)
            var multiDamage = new DamageSpecifier();
            foreach (var type in DamageTypesToBeTested)
            {
                multiDamage += new DamageSpecifier(ProtoMan.Index(type), 5);
            }
            //Damage the urist
            damageableSystem.SetDamage((STarget.Value, damageableComp), multiDamage);

            //Use topical
            await Interact();
            //Assert correct damage done
            correctDamage = multiDamage + healsTypes;
            correctDamage.ClampMin(0); //in case damage became negative somehow
            correctDamage.TrimZeros();
            Assert.That(damageableSystem.GetPositiveDamage((STarget.Value, damageableComp)).Equals(correctDamage),
            "The topical did not heal the correct damage amongst many damage types");

            //Try to use the topical with no more matching damage types
            await Interact(false);
            //Assert could not use (by checking if there are any doafters)
            Assert.That(!ActiveDoAfters.Any(),
            "Topical use went through despite wrong damage type.");
            //Assert Other damage is unchanged
            Assert.That(damageableSystem.GetPositiveDamage((STarget.Value, damageableComp)).Equals(correctDamage),
            "The topical healed damage it shouldn't.");

            //Set topical stack size to 2
            sharedStackSystem.SetCount((topicalUid, stackComp), 2);
            //Assert stack count is 2
            Assert.That(stackComp.Count == 2,
            "The stack of topicals did not get set to 2");

            //Deal 3 topicals worth to the urist
            damageAmount = healsTypes * -3;
            damageableSystem.SetDamage((STarget.Value, damageableComp), damageAmount);

            //Use topicals until we run out
            await Interact();

            //Assert Hands empty
            var inHands = HandSys.GetActiveItem((SPlayer, Hands));
            Assert.That(inHands, Is.Null,
            "Topicals did not leave hand after being used up.");

            //Assert some brute damage remains
            correctDamage = -healsTypes;
            Assert.That(damageableSystem.GetPositiveDamage((STarget.Value, damageableComp)).Equals(correctDamage),
            "The topical stack did not leave the correct amount of damage after running out");
        }
    }
}
