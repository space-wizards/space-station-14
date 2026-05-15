#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.Utility;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests;

/// <summary>
/// Tests to see if any entity prototypes specify solution fill level sprites that don't exist.
/// </summary>
public sealed class FillLevelSpriteTest : GameTest
{
    private static readonly string[] HandStateNames = ["left", "right"];
    private static readonly string[] EquipStateNames = ["back", "suitstorage"];

    private static readonly string[] SolutionContainerVisualsEntities = GameDataScrounger.EntitiesWithComponent("SolutionContainerVisuals");

    [SidedDependency(Side.Client)] private SpriteSystem _cSpriteSystem = null!;

    [TestCaseSource(nameof(SolutionContainerVisualsEntities))]
    [Description("Tests to see if any entity prototypes specify solution fill level sprites that don't exist.")]
    [RunOnSide(Side.Client)]
    public async Task FillLevelSpritesExist(string protoId)
    {
        var proto = CProtoMan.Index(protoId);

        proto.TryGetComponent<SolutionContainerVisualsComponent>(out var visuals, CEntMan.ComponentFactory);
        proto.TryGetComponent<SpriteComponent>(out var sprite, CEntMan.ComponentFactory);
        proto.TryGetComponent<AppearanceComponent>(out var appearance, CEntMan.ComponentFactory);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(sprite, Is.Not.Null, $"{proto.ID} has {nameof(SolutionContainerVisualsComponent)} but no {nameof(SpriteComponent)}.");
            Assert.That(appearance, Is.Not.Null, $"{proto.ID} has {nameof(SolutionContainerVisualsComponent)} but no {nameof(AppearanceComponent)}.");
        }

        // Test base sprite fills
        if (!string.IsNullOrEmpty(visuals!.FillBaseName) && visuals.MaxFillLevels > 0)
        {
            var entity = CSpawn(proto.ID);
            if (!_cSpriteSystem.LayerMapTryGet(entity, SolutionContainerLayers.Fill, out var fillLayerId, false))
            {
                Assert.Fail($"{proto.ID} has {nameof(SolutionContainerVisualsComponent)} but no fill layer map.");
            }
            if (!_cSpriteSystem.TryGetLayer(entity, fillLayerId, out var fillLayer, false))
            {
                Assert.Fail($"{proto.ID} somehow lost a layer.");
            }
            var rsi = fillLayer!.ActualRsi;

            for (var i = 1; i <= visuals.MaxFillLevels; i++)
            {
                var state = $"{visuals.FillBaseName}{i}";
                Assert.That(rsi!.TryGetState(state, out _), @$"{proto.ID} has {nameof(SolutionContainerVisualsComponent)} with
                    {nameof(SolutionContainerVisualsComponent.MaxFillLevels)} = {visuals.MaxFillLevels}, but {rsi.Path} doesn't have state {state}!");
            }
        }

        // Test inhand sprite fills
        if (!string.IsNullOrEmpty(visuals.InHandsFillBaseName) && visuals.InHandsMaxFillLevels > 0)
        {
            var rsi = sprite.BaseRSI;
            for (var i = 1; i <= visuals.InHandsMaxFillLevels; i++)
            {
                foreach (var handname in HandStateNames)
                {
                    var state = $"inhand-{handname}{visuals.InHandsFillBaseName}{i}";
                    Assert.That(rsi!.TryGetState(state, out _), @$"{proto.ID} has {nameof(SolutionContainerVisualsComponent)} with
                        {nameof(SolutionContainerVisualsComponent.InHandsMaxFillLevels)} = {visuals.InHandsMaxFillLevels}, but {rsi.Path} doesn't have state {state}!");
                }
            }
        }

        // Test equipped sprite fills
        if (!string.IsNullOrEmpty(visuals.EquippedFillBaseName) && visuals.EquippedMaxFillLevels > 0)
        {
            var rsi = sprite.BaseRSI;
            for (var i = 1; i <= visuals.EquippedMaxFillLevels; i++)
            {
                foreach (var equipName in EquipStateNames)
                {
                    var state = $"equipped-{equipName}{visuals.EquippedFillBaseName}{i}";
                    Assert.That(rsi!.TryGetState(state, out _), @$"{proto.ID} has {nameof(SolutionContainerVisualsComponent)} with
                        {nameof(SolutionContainerVisualsComponent.EquippedMaxFillLevels)} = {visuals.EquippedMaxFillLevels}, but {rsi.Path} doesn't have state {state}!");
                }
            }
        }
    }
}
