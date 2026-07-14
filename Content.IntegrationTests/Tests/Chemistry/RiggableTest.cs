using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Power.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Chemistry;

[TestOf(typeof(RiggableSystem))]
public sealed class RiggableTest : InteractionTest
{
    private static readonly EntProtoId HumanProtoId = "MobHuman";
    private static readonly EntProtoId BatteryProto = "PowerCellSmall";
    private static readonly EntProtoId FlashlightProto = "EmptyFlashlightLantern";
    private static readonly EntProtoId StunbatonProto = "Stunbaton";

    [SidedDependency(Side.Server)] private readonly DamageableSystem _damageable = default!;

    [TestPrototypes]
    private const string Prototypes = @"
- type: entity
  parent: PrefilledSyringe
  id: TestPlasmaSyringe
  components:
  - type: Solution
    solution:
      reagents:
      - ReagentId: Plasma
        Quantity: 15

- type: entity
  parent: PrefilledSyringe
  id: TestMilkSyringe
  components:
  - type: Solution
    solution:
      reagents:
      - ReagentId: Milk
        Quantity: 15
";

    [Test]
    [TestCase("TestPlasmaSyringe", ExpectedResult = true)]
    [TestCase("TestMilkSyringe", ExpectedResult = false)]
    [Description("Gives the player a power cell, injects it with different solutions and tests the rigged cell in a flashlight")]
    public async Task<bool> RigBatteryTest(string syringe)
    {
        await AddAtmosphere();
        await SpawnTarget(HumanProtoId);

        Entity<DamageableComponent> mob = (STarget.Value, Comp<DamageableComponent>());
        Assert.That(_damageable.GetPositiveDamage(mob).GetTotal(), Is.EqualTo(FixedPoint2.Zero),
            "Player spawned with damage.");

        var battery = await SpawnTarget(BatteryProto);

        // Rig the cell
        await PlaceInHands(syringe);
        await Interact();

        // Put it into a flashlight
        await Pickup(battery);
        await SpawnTarget(FlashlightProto);
        await Interact();
        await RunTicks(5);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(HandSys.GetActiveItem((SPlayer, Hands)), Is.Null,
                "Battery did not get inserted into the flashlight.");
            Assert.That(_damageable.GetPositiveDamage(mob).GetTotal(), Is.EqualTo(FixedPoint2.Zero),
                "Player received damage before flashlight activation.");
        }

        // Turn the flashlight on and observe the result
        await Activate();
        await RunTicks(5);

        if (_damageable.GetPositiveDamage(mob).GetTotal() > FixedPoint2.Zero)
        {
            // Flashlight exploded
            AssertDeleted(battery);
            return true;
        }
        else
        {
            // Nothing happened
            return false;
        }
    }

    [Test]
    [Description("Gives the player an activated stunbaton and tests that it explodes immediately on plasma injection")]
    public async Task RigActivatedTest()
    {
        await AddAtmosphere();
        await SpawnTarget(HumanProtoId);

        Entity<DamageableComponent> mob = (STarget.Value, Comp<DamageableComponent>());
        Assert.That(_damageable.GetPositiveDamage(mob).GetTotal(), Is.EqualTo(FixedPoint2.Zero),
            "Player spawned with damage.");

        var baton = await PlaceInHands(StunbatonProto);

        Assert.That(Comp<ItemToggleComponent>(baton).Activated, Is.True, "Stunbaton did not activate");

        // Rig the baton
        await PlaceInHands("TestPlasmaSyringe");
        await Interact();

        await RunTicks(5);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_damageable.GetPositiveDamage(mob).GetTotal(), Is.GreaterThan(FixedPoint2.Zero), "Rigged stunbaton did not explode?");
            AssertDeleted(baton);
        }
    }
}
