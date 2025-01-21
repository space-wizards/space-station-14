using System.Linq;
using Content.Client.Weapons.Ranged.Components;
using Content.Shared.Prototypes;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests;

/// <summary>
/// Tests all entity prototypes with the MagazineVisualsComponent.
/// </summary>
[TestFixture]
public sealed class MagazineVisualsSpriteTest
{
    [Test]
    public async Task MagazineVisualsSpritesExist()
    {
        await using var pair = await PoolManager.GetServerClient();
        var client = pair.Client;
        var protoMan = client.ResolveDependency<IPrototypeManager>();
        var componentFactory = client.ResolveDependency<IComponentFactory>();

        await client.WaitAssertion(() =>
        {
            var protos = protoMan.EnumeratePrototypes<EntityPrototype>()
                .Where(p => !p.Abstract)
                .Where(p => !pair.IsTestPrototype(p))
                .Where(p => p.HasComponent<MagazineVisualsComponent>(componentFactory));

            foreach (var proto in protos)
            {
                Assert.That(proto.TryGetComponent<MagazineVisualsComponent>(out var visuals, componentFactory));
                Assert.That(proto.TryGetComponent<SpriteComponent>(out var sprite, componentFactory));
                if (!proto.HasComponent<AppearanceComponent>(componentFactory))
                {
                    Assert.Fail(@$"{proto.ID} has MagazineVisualsComponent but no AppearanceComponent.");
                }

                var hasMag = sprite.LayerMapTryGet(GunVisualLayers.Mag, out var magLayerId);
                var hasUnshadedMag = sprite.LayerMapTryGet(GunVisualLayers.MagUnshaded, out var magUnshadedLayerId);

                if (!hasMag && !hasUnshadedMag)
                {
                    Assert.Fail(@$"{proto.ID} has MagazineVisualsComponent but no Mag or MagUnshaded layer map.");
                }

                var start = visuals.ZeroVisible ? 0 : 1;
                for (var i = start; i < visuals.MagSteps; i++)
                {
                    if (hasMag)
                    {
                        Assert.That(sprite.TryGetLayer(magLayerId, out var layer));
                        var rsi = layer.ActualRsi;
                        var state = $"{visuals.MagState}-{i}";

                        Assert.That(rsi.TryGetState(state, out _), @$"{proto.ID} has MagazineVisualsComponent with
                                    MagSteps = {visuals.MagSteps}, but {rsi.Path} doesn't have state {state}!");

                        // MagSteps includes the 0th step, so sometimes people are off by one.
                        var extraState = $"{visuals.MagState}-{visuals.MagSteps}";
                        Assert.That(!rsi.TryGetState(extraState, out _), @$"{proto.ID} has MagazineVisualsComponent with
                                        MagSteps = {visuals.MagSteps}, but more states exist!");
                    }
                    if (hasUnshadedMag)
                    {
                        Assert.That(sprite.TryGetLayer(magUnshadedLayerId, out var layer));
                        var rsi = layer.ActualRsi;
                        var state = $"{visuals.MagState}-unshaded-{i}";

                        Assert.That(rsi.TryGetState(state, out _), @$"{proto.ID} has MagazineVisualsComponent with
                                    MagSteps = {visuals.MagSteps}, but {rsi.Path} doesn't have state {state}!");

                        // MagSteps includes the 0th step, so sometimes people are off by one.
                        var extraState = $"{visuals.MagState}-unshaded-{visuals.MagSteps}";
                        Assert.That(!rsi.TryGetState(extraState, out _), @$"{proto.ID} has MagazineVisualsComponent with
                                        MagSteps = {visuals.MagSteps}, but more states exist!");
                    }
                }
            }
        });

        await pair.CleanReturnAsync();
    }
}
