using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;

namespace Content.IntegrationTests.Tests.Humanoid;

[TestOf(typeof(SharedHideableHumanoidLayersSystem))]
public sealed class HideableHumanoidLayersTest : InteractionTest
{
    protected override string PlayerPrototype => "MobReptilian";

    [Test]
    public async Task BasicHiding()
    {
        await SpawnTarget("ClothingMaskGas");
        await Pickup(); // equip mask
        await UseInHand();

        await Server.WaitAssertion(() =>
        {
            var hideableHumanoidLayers = SEntMan.GetComponent<HideableHumanoidLayersComponent>(SPlayer);
            Assert.That(hideableHumanoidLayers.HiddenLayers, Does.ContainKey(HumanoidVisualLayers.Snout).WithValue(SlotFlags.MASK));
        });

        await Server.WaitAssertion(() =>
        {
            SEntMan.DeleteEntity(STarget); // de-equip mask

            var hideableHumanoidLayers = SEntMan.GetComponent<HideableHumanoidLayersComponent>(SPlayer);
            Assert.That(hideableHumanoidLayers.HiddenLayers, Does.Not.ContainKey(HumanoidVisualLayers.Snout));
        });
    }
}
