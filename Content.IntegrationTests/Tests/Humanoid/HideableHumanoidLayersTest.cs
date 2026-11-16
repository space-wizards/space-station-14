#nullable enable
using System.Collections.Generic;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Body;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Inventory;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Humanoid;

[TestOf(typeof(SharedHideableHumanoidLayersSystem))]
public sealed class HideableHumanoidLayersTest : InteractionTest
{
    protected override string PlayerPrototype => "MobVulpkanin";
    private static readonly EntProtoId ClothingMaskGas = "ClothingMaskGas";

    [SidedDependency(Side.Client)] private SpriteSystem _cSpriteSystem = default!;
    [SidedDependency(Side.Server)] private SharedVisualBodySystem _sVisualBodySystem = default!;

    [Test]
    public async Task BasicHiding()
    {
        await SpawnTarget(ClothingMaskGas);
        await Pickup(); // equip mask
        await UseInHand();

        await Server.WaitAssertion(() =>
        {
            var hideableHumanoidLayers = SComp<HideableHumanoidLayersComponent>(SPlayer);
            Assert.That(hideableHumanoidLayers.HiddenLayers, Does.ContainKey(HumanoidVisualLayers.Snout).WithValue(SlotFlags.MASK));
        });

        await Server.WaitAssertion(() =>
        {
            SDeleteNow(STarget.Value); // de-equip mask

            var hideableHumanoidLayers = SComp<HideableHumanoidLayersComponent>(SPlayer);
            Assert.That(hideableHumanoidLayers.HiddenLayers, Does.Not.ContainKey(HumanoidVisualLayers.Snout));
        });
    }

    [Test]
    public async Task DependentHiding()
    {
        await Server.WaitAssertion(() =>
        {
            _sVisualBodySystem.ApplyMarkings(SPlayer, new()
            {
                ["Head"] = new()
                {
                    [HumanoidVisualLayers.SnoutCover] = [new("VulpSnoutNose", 1)],
                },
            });
        });

        await SpawnTarget(ClothingMaskGas);
        await Pickup(); // equip mask
        await UseInHand();

        await RunTicks(20);

        await Client.WaitAssertion(() =>
        {
            var snoutIndex = _cSpriteSystem.LayerMapGet(CPlayer, "VulpSnout-snout");
            var snoutCoverIndex = _cSpriteSystem.LayerMapGet(CPlayer, "VulpSnoutNose-snout-nose");
            var spriteComp = CComp<SpriteComponent>(CPlayer);

            Assert.That(spriteComp[snoutIndex].Visible, Is.False);
            Assert.That(spriteComp[snoutCoverIndex].Visible, Is.False);
        });

        await Server.WaitAssertion(() =>
        {
            SDeleteNow(STarget.Value); // de-equip mask
        });

        await RunTicks(20);

        await Client.WaitAssertion(() =>
        {
            var snoutIndex = _cSpriteSystem.LayerMapGet(CPlayer, "VulpSnout-snout");
            var snoutCoverIndex = _cSpriteSystem.LayerMapGet(CPlayer, "VulpSnoutNose-snout-nose");
            var spriteComp = CComp<SpriteComponent>(CPlayer);

            Assert.That(spriteComp[snoutIndex].Visible, Is.True);
            Assert.That(spriteComp[snoutCoverIndex].Visible, Is.True);
        });
    }
}
