using System.Linq;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Damage;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Storage;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.ToolQualities;

public sealed class SlicingTests : InteractionTest
{
    private const string ButcherableTarget = "MobCorgiIan";
    private const string SliceableTarget = "Log";
    private const string NonsliceableTarget = "LockerCaptain";

    [Test]
    public async Task TestButcherableSystemOnDeadTarget()
    {
        var damageableSystem = SEntMan.System<DamageableSystem>();
        var mobStateSystem = SEntMan.System<MobStateSystem>();

        // Spawn Ian
        await SpawnTarget(ButcherableTarget);
        AssertExists(Target);

        // Kill him with a brick or something
        var butcherableComp = Comp<ButcherableComponent>();
        var mobStateComp = Comp<MobStateComponent>();
        var mobThresholdsComp = Comp<MobThresholdsComponent>();
        var damageableComp = Comp<DamageableComponent>();
        var lethalDamageThreshold = mobThresholdsComp.Thresholds.Keys.Last();
        var lethalDamage = new DamageSpecifier();
        lethalDamage.DamageDict.Add("Blunt", lethalDamageThreshold);
        await Server.WaitAssertion(() =>
        {
            damageableSystem.SetDamage(STarget.Value, damageableComp, lethalDamage);
            Assert.That(mobStateSystem.IsDead(STarget.Value, mobStateComp));
        });

        // Try to butcher the dead dog
        await InteractUsing(Slicing);
        AssertDeleted(Target);

        var spawnedEntities = new EntitySpecifierCollection();
        spawnedEntities.Add("FoodMeatCorgi", 2);
        spawnedEntities.Add("MaterialHideCorgi", 1);
        // we're looking for items, so we use sundries
        // looking for everything will include the blood puddles that spawn as well and trip the assertion
        await AssertEntityLookup(spawnedEntities, flags: LookupFlags.Sundries);
    }

    [Test]
    public async Task TestButcherableSystemOnLivingTarget()
    {
        // Spawn Ian
        await SpawnTarget(ButcherableTarget);
        AssertExists(Target);

        // Don't beat him with a brick

        // Try to butcher the living dog
        await InteractUsing(Slicing);
        AssertExists(Target);
    }

    [Test]
    public async Task TestSliceableSystemOnSliceableEntity()
    {
        await SpawnTarget(SliceableTarget);
        AssertExists(Target);

        await InteractUsing(Slicing);
        AssertDeleted(Target);

        var spawnedEntities = new EntitySpecifierCollection();
        spawnedEntities.Add("MaterialWoodPlank1", 2);
        await AssertEntityLookup(spawnedEntities);
    }

    [Test]
    public async Task TestSliceableSystemOnNonsliceableEntity()
    {
        await SpawnTarget(NonsliceableTarget);
        AssertExists(Target);

        // Sadly we can't katana slash our way into the Captain's locker
        await InteractUsing(Slicing);
        AssertExists(Target);
    }
}
