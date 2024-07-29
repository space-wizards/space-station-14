using System.Linq;
using Content.Shared.Chemistry.Components;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests;

/// <summary>
/// Tests to see if any entity prototypes specify solution fill level sprites that don't exist.
/// </summary>
[TestFixture]
public sealed class FillLevelSpriteTest
{
    private static readonly string[] HandStateNames = ["left", "right"];

    [Test]
    public async Task FillLevelSpritesExist()
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
                .Where(p => p.TryGetComponent<SolutionContainerVisualsComponent>(out _, componentFactory))
                .OrderBy(p => p.ID)
                .ToList();

            foreach (var proto in protos)
            {
                Assert.That(proto.TryGetComponent<SolutionContainerVisualsComponent>(out var visuals, componentFactory));
                Assert.That(proto.TryGetComponent<SpriteComponent>(out var sprite, componentFactory));

                var rsi = sprite.BaseRSI;

                // Test base sprite fills
                if (!string.IsNullOrEmpty(visuals.FillBaseName))
                {
                    for (var i = 1; i <= visuals.MaxFillLevels; i++)
                    {
                        var state = $"{visuals.FillBaseName}{i}";
                        Assert.That(rsi.TryGetState(state, out _), @$"{proto.ID} has SolutionContainerVisualsComponent with
                            MaxFillLevels = {visuals.MaxFillLevels}, but {rsi.Path} doesn't have state {state}!");
                    }
                }

                // Test inhand sprite fills
                if (!string.IsNullOrEmpty(visuals.InHandsFillBaseName))
                {
                    for (var i = 1; i <= visuals.InHandsMaxFillLevels; i++)
                    {
                        foreach (var handname in HandStateNames)
                        {
                            var state = $"inhand-{handname}{visuals.InHandsFillBaseName}{i}";
                            Assert.That(rsi.TryGetState(state, out _), @$"{proto.ID} has SolutionContainerVisualsComponent with
                                InHandsMaxFillLevels = {visuals.InHandsMaxFillLevels}, but {rsi.Path} doesn't have state {state}!");
                        }

                    }
                }
            }
        });

        await pair.CleanReturnAsync();
    }
}
