using System.Collections.Generic;
using Content.Client.Weapons.Ranged.Components;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;

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
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true });
        var client = pair.Client;
        var toTest = new List<(int, string)>();
        var protos = pair.GetPrototypesWithComponent<MagazineVisualsComponent>();
        var spriteSys = client.System<SpriteSystem>();

        await client.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                foreach (var (proto, _) in protos)
                {
                    var uid = client.EntMan.Spawn(proto.ID);
                    var visuals = client.EntMan.GetComponent<MagazineVisualsComponent>(uid);

                    Assert.That(client.EntMan.TryGetComponent(uid, out SpriteComponent sprite),
                        @$"{proto.ID} has MagazineVisualsComponent but no SpriteComponent.");
                    Assert.That(client.EntMan.HasComponent<AppearanceComponent>(uid),
                        @$"{proto.ID} has MagazineVisualsComponent but no AppearanceComponent.");

                    toTest.Clear();
                    if (spriteSys.LayerMapTryGet((uid, sprite), GunVisualLayers.Mag, out var magLayerId, false))
                        toTest.Add((magLayerId, ""));
                    if (spriteSys.LayerMapTryGet((uid, sprite), GunVisualLayers.MagUnshaded, out var magUnshadedLayerId, false))
                        toTest.Add((magUnshadedLayerId, "-unshaded"));

                    Assert.That(
                        toTest,
                        Is.Not.Empty,
                        @$"{proto.ID} has MagazineVisualsComponent but no Mag or MagUnshaded layer map.");

                    var start = visuals.ZeroVisible ? 0 : 1;
                    foreach (var (id, midfix) in toTest)
                    {
                        Assert.That(spriteSys.TryGetLayer((uid, sprite), id, out var layer, false));
                        var rsi = layer.ActualRsi;
                        for (var i = start; i < visuals.MagSteps; i++)
                        {
                            var state = $"{visuals.MagState}{midfix}-{i}";
                            Assert.That(rsi.TryGetState(state, out _),
                                @$"{proto.ID} has MagazineVisualsComponent with MagSteps = {visuals.MagSteps}, but {rsi.Path} doesn't have state {state}!");
                        }

                        // MagSteps includes the 0th step, so sometimes people are off by one.
                        var extraState = $"{visuals.MagState}{midfix}-{visuals.MagSteps}";
                        Assert.That(rsi.TryGetState(extraState, out _), Is.False,
                            @$"{proto.ID} has MagazineVisualsComponent with MagSteps = {visuals.MagSteps}, but more states exist!");

                        client.EntMan.DeleteEntity(uid);
                    }
                }
            });
        });

        await pair.CleanReturnAsync();
    }
}
