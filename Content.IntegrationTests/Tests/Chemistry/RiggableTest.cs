using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Chemistry;

public sealed class RiggableTest : InteractionTest
{
    // Both the user and the explosion victim
    private static readonly EntProtoId HumanProtoId = "MobHuman";
    private static readonly EntProtoId BatteryProto = "PowerCellSmall";
    private static readonly EntProtoId FlashlightProto = "EmptyFlashlightLantern";

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
    public async Task<bool> RigBatteryTest(string syringe)
    {
        var damageSys = SEntMan.System<DamageableSystem>();

        await AddAtmosphere();
        await SpawnTarget(HumanProtoId);

        Entity<DamageableComponent> mob = (STarget.Value, Comp<DamageableComponent>());
        Assert.That(damageSys.GetPositiveDamage(mob).GetTotal(), Is.EqualTo(FixedPoint2.Zero),
            "Player spawned with damage.");

        var battery = await SpawnTarget(BatteryProto);

        // Rig the cell
        await PlaceInHands(syringe);
        await Interact();

        // Put it into a flashlight
        await Pickup(battery);
        await SpawnTarget(FlashlightProto);
        await Interact();

        Assert.That(HandSys.GetActiveItem((SPlayer, Hands)), Is.Null,
            "Battery did not get inserted into the flashlight.");

        // Turn the flashlight on and observe the result
        await Activate();
        await RunTicks(5);

        if (damageSys.GetPositiveDamage(mob).GetTotal() > FixedPoint2.Zero)
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
}
