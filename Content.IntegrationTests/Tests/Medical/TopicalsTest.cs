#nullable enable
using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Medical.Healing;
using Content.IntegrationTests.Utility;
using Content.Shared.Stacks;

using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.GameObjects;
using Content.Client.Body.Systems;
using Content.Shared.Body.Systems;
using Content.Shared.Body.Components;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Robust.Shared.Utility;

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

    private static string[] _topicalsToBeTested = GameDataScrounger.EntitiesWithComponent("Healing");
    private static string[] _allDamageTypes = GameDataScrounger.PrototypesOfKind("damageType");

    /// <summary>
    /// Tests that all topicals heal the right damage, cannot be used on incorrect damage, lower with stack size on use, and stop healing after being used up"
    /// </summary>
    [Test]
    [TestOf(typeof(HealingComponent))]
    [TestCaseSource(nameof(_topicalsToBeTested))]
    [Description("Tests that all topicals heal the right damage, cannot be used on incorrect damage, lower with stack size on use, and stop healing after being used up")]
    public async Task TopicalsInteractionsTest(string protoKey)
    {
        var damageableSystem = SEntMan.System<DamageableSystem>();
        var sharedStackSystem = SEntMan.System<SharedStackSystem>();
        var bloodStreamSystem = SEntMan.System<SharedBloodstreamSystem>();

        //make a damage specifier with 5 of all normal types of damage (exclude structural and holy)
        var allNormalDamageSpecifier = new DamageSpecifier();
        foreach (var type in _allDamageTypes)
        {
            ProtoId<DamageTypePrototype> typeID = type;
            if (type != "Structural" && type != "Holy")
                allNormalDamageSpecifier += new DamageSpecifier(ProtoMan.Index(typeID), 5);
        }
        var emptyDamageSpecifier = new DamageSpecifier();
        var damageAmount = new DamageSpecifier();

        await AddAtmosphere(); // prevent everyone from suffocating

        //Spawn a new urist, with empty health
        var urist = await SpawnTarget(MobHuman);
        var damageableComp = Comp<DamageableComponent>(urist);
        damageableSystem.SetDamage((STarget.Value, damageableComp), damageAmount);

        var bloodStreamComp = Comp<BloodstreamComponent>(urist);
        //Stop passive healing from messing with the numbers
        var passivehealing = Comp<PassiveDamageComponent>(urist);
        passivehealing.Damage = new();


        //Hold the topical stack
        var topical = await PlaceInHands(protoKey);
        EntityUid? inHands;
        var topicalUid = SEntMan.GetEntity(topical);
        var healingComp = Comp<HealingComponent>(topical);
        //get the damage types this is meant to fix
        DamageSpecifier healsTypes = healingComp.Damage;
        DamageSpecifier correctDamage;

        //Check to see if the topical deals damage to you
        var dealsDamage = false;
        foreach (var (key, value) in healsTypes.DamageDict)
        {
            if (value > 0)
            {
                dealsDamage = true;
                break;
            }
        }
        if (dealsDamage) //we need special logic if it's going to hurt you to use (looking at you tourniquet)
        {
            //Bleed the amount the topical fixes
            bloodStreamSystem.TryModifyBleedAmount((STarget.Value, bloodStreamComp), healingComp.BloodlossModifier);
            //Use our topical
            await Interact();
            Assert.That(damageableSystem.GetPositiveDamage((STarget.Value, damageableComp)), Is.EqualTo(healingComp.Damage),
            "The self-damaging topical did not do the correct amount of damage.");
            Assert.That(bloodStreamSystem.GetBloodLevel((STarget.Value, bloodStreamComp)), Is.EqualTo(0),
            "The self-damaging topical did not fix bleeding fully.");
            return;
        }

        //Damage the Urist 2 topicals worth
        damageAmount = healsTypes * -2;
        damageableSystem.SetDamage((STarget.Value, damageableComp), damageAmount);
        Assert.That(damageableSystem.GetPositiveDamage((STarget.Value, damageableComp)).Equals(damageAmount),
        "The urist did not get damaged the correct amount.");

        //if we do not have a stack comp (looking at you healingtoolbox)
        if (!HasComp<StackComponent>(topical))
        {
            //Use our topical
            await Interact();
            //Assert that all
            Assert.That(damageableSystem.GetPositiveDamage((STarget.Value, damageableComp)), Is.EqualTo(emptyDamageSpecifier),
            "The topical did not heal all of its damage types.");
            return;
        }

        //For keeping track of stack count.
        var stackComp = Comp<StackComponent>(topical);
        var startStackSize = stackComp.Count;

        //if the topical affects blood loss damage
        if (healingComp.Damage.DamageDict.ContainsKey("Bloodloss"))
        {
            //Bleed the amount the topical fixes
            bloodStreamSystem.TryModifyBleedAmount((STarget.Value, bloodStreamComp), -healingComp.BloodlossModifier);
            //Use up our topicals
            await Interact();
            //Assert that all damage and blood loss is fixed
            Assert.That(damageableSystem.GetPositiveDamage((STarget.Value, damageableComp)), Is.EqualTo(emptyDamageSpecifier),
            "The blood-related topical did not heal all of its damage types.");
            Assert.That(bloodStreamSystem.GetBloodLevel((STarget.Value, bloodStreamComp)), Is.EqualTo(0),
            "The blood-related topical did not fix bleeding fully.");
            return;
        }
        if (startStackSize == 1)
        {
            //Use up our one topical
            await Interact();
            //Assert Hands empty
            inHands = HandSys.GetActiveItem((SPlayer, Hands));
            Assert.That(inHands, Is.Null,
            "Topicals did not leave hand after being used up.");

            //Assert that each correct Damage type lowered by one topical's worth
            correctDamage = -healsTypes;
            Assert.That(damageableSystem.GetPositiveDamage((STarget.Value, damageableComp)), Is.EqualTo(correctDamage),
            "The correct damage types did not lower, or did not lower the correct amount.");
        }
        else //if we have a stack of more than 1
        {
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
            correctDamage = -healsTypes;
            Assert.That(damageableSystem.GetPositiveDamage((STarget.Value, damageableComp)), Is.EqualTo(correctDamage),
            "The correct damage types did not lower, or did not lower the correct amount.");

            //Use topical
            await Interact();
            //Assert that all damage gone
            Assert.That(damageableSystem.GetPositiveDamage((STarget.Value, damageableComp)), Is.EqualTo(emptyDamageSpecifier),
            "Damage was not fully removed after using topical twice.");

            //Find a damage type that the topical does not heal
            ProtoId<DamageTypePrototype>? incorrectDamageType = null;
            foreach (var (key, value) in allNormalDamageSpecifier.DamageDict)
            {
                if (!healsTypes.DamageDict.ContainsKey(key))
                {
                    incorrectDamageType = key;
                    break;
                }
            }
            //Only run this test if we found an incorrect damage type
            if (!(incorrectDamageType == null))
            {   //Damage 10 in the incorrect Damage Type
                var incorrectDamageSpecifier = new DamageSpecifier(ProtoMan.Index(incorrectDamageType), 10);
                damageableSystem.SetDamage((STarget.Value, damageableComp), incorrectDamageSpecifier);
                //Try to use the topical
                await Interact(false);
                //Assert could not use (by checking if there are any doafters)
                Assert.That(!ActiveDoAfters.Any(),
                "Topical use started despite wrong damage type.");
                //Assert incorrect damage is unchanged
                Assert.That(damageableSystem.GetPositiveDamage((STarget.Value, damageableComp)), Is.EqualTo(incorrectDamageSpecifier),
                "The topical healed damage it shouldn't.");
            }


            //Testing Multiple Damage Types (5 of each excluding Structural and Holy)
            //Damage the urist
            damageableSystem.SetDamage((STarget.Value, damageableComp), allNormalDamageSpecifier);
            //Use topical until we can't anymore
            await Interact();
            //Assert correct damage done
            correctDamage = allNormalDamageSpecifier + healsTypes * 10; //healtypes is negative, so this makes all correct types negative
            correctDamage.ClampMin(0); // which can then be clamped to zero
            correctDamage.TrimZeros();
            Assert.That(damageableSystem.GetPositiveDamage((STarget.Value, damageableComp)), Is.EqualTo(correctDamage),
            "The topical did not heal the correct damage amongst many damage types");

            //Try to use the topical with no more matching damage types
            await Interact(false);
            //Assert could not use (by checking if there are any doafters)
            Assert.That(!ActiveDoAfters.Any(),
            "Topical use went through despite wrong damage type.");
            //Assert Other damage is unchanged
            Assert.That(damageableSystem.GetPositiveDamage((STarget.Value, damageableComp)), Is.EqualTo(correctDamage),
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
            inHands = HandSys.GetActiveItem((SPlayer, Hands));
            Assert.That(inHands, Is.Null,
            "Topicals did not leave hand after being used up.");

            //Assert some correct damage remains
            correctDamage = -healsTypes;
            Assert.That(damageableSystem.GetPositiveDamage((STarget.Value, damageableComp)), Is.EqualTo(correctDamage),
            "The topical stack did not leave the correct amount of damage after running out");
        }
    }
}
