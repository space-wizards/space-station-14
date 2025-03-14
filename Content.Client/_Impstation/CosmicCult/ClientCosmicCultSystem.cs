using Content.Shared._Impstation.CosmicCult.Components;
using Content.Shared._Impstation.CosmicCult;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;
using Content.Shared._Impstation.CosmicCult.Components.Examine;
using System.Numerics;
using Timer = Robust.Shared.Timing.Timer;
using Robust.Client.Audio;
using Robust.Shared.Audio;

namespace Content.Client._Impstation.CosmicCult;

public sealed partial class CosmicCultSystem : SharedCosmicCultSystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    private readonly ResPath _rsiPath = new("/Textures/_Impstation/CosmicCult/Effects/ability_siphonvfx.rsi");
    private readonly SoundSpecifier _siphonSFX = new SoundPathSpecifier("/Audio/_Impstation/CosmicCult/ability_siphon.ogg");
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RogueAscendedInfectionComponent, ComponentStartup>(OnAscendedInfectionAdded);
        SubscribeLocalEvent<RogueAscendedInfectionComponent, ComponentShutdown>(OnAscendedInfectionRemoved);

        SubscribeLocalEvent<RogueAscendedAuraComponent, ComponentStartup>(OnAscendedAuraAdded);
        SubscribeLocalEvent<RogueAscendedAuraComponent, ComponentShutdown>(OnAscendedAuraRemoved);

        SubscribeLocalEvent<CosmicStarMarkComponent, ComponentStartup>(OnCosmicStarMarkAdded);
        SubscribeLocalEvent<CosmicStarMarkComponent, ComponentShutdown>(OnCosmicStarMarkRemoved);

        SubscribeLocalEvent<CosmicImposingComponent, ComponentStartup>(OnCosmicImpositionAdded);
        SubscribeLocalEvent<CosmicImposingComponent, ComponentShutdown>(OnCosmicImpositionRemoved);

        SubscribeLocalEvent<CosmicCultComponent, GetStatusIconsEvent>(GetCosmicCultIcon);
        SubscribeLocalEvent<CosmicCultLeadComponent, GetStatusIconsEvent>(GetCosmicCultLeadIcon);
        SubscribeLocalEvent<CosmicMarkBlankComponent, GetStatusIconsEvent>(GetCosmicSSDIcon);

        SubscribeNetworkEvent<CosmicSiphonIndicatorEvent>(OnSiphon);
    }
    private void OnSiphon(CosmicSiphonIndicatorEvent args)
    {
        var ent = GetEntity(args.Target);
        if (TryComp<SpriteComponent>(ent, out var sprite))
        {
            var layer = sprite.AddLayer(new SpriteSpecifier.Rsi(_rsiPath, "vfx"));
            sprite.LayerMapSet(CultSiphonedVisuals.Key, layer);
            sprite.LayerSetOffset(layer, new Vector2(0, 0.8f));
            sprite.LayerSetScale(layer, new Vector2(0.65f, 0.65f));
            sprite.LayerSetShader(layer, "unshaded");

            Timer.Spawn(TimeSpan.FromSeconds(2), () => sprite.RemoveLayer(CultSiphonedVisuals.Key));
            _audio.PlayLocal(_siphonSFX, ent, ent, AudioParams.Default.WithVariation(0.1f));
        }
    }

    #region Additions
    private void OnAscendedInfectionAdded(Entity<RogueAscendedInfectionComponent> uid, ref ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite) || sprite.LayerMapTryGet(AscendedInfectionKey.Key, out _))
            return;

        var layer = sprite.AddLayer(new SpriteSpecifier.Rsi(uid.Comp.RsiPath, uid.Comp.States));

        sprite.LayerMapSet(AscendedInfectionKey.Key, layer);
        sprite.LayerSetShader(layer, "unshaded");
    }

    private void OnAscendedAuraAdded(Entity<RogueAscendedAuraComponent> uid, ref ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite) || sprite.LayerMapTryGet(AscendedAuraKey.Key, out _))
            return;

        var layer = sprite.AddLayer(new SpriteSpecifier.Rsi(uid.Comp.RsiPath, uid.Comp.States));

        sprite.LayerMapSet(AscendedAuraKey.Key, layer);
        sprite.LayerSetShader(layer, "unshaded");
    }
    private void OnCosmicStarMarkAdded(Entity<CosmicStarMarkComponent> uid, ref ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite) || sprite.LayerMapTryGet(CosmicRevealedKey.Key, out _))
            return;

        var layer = sprite.AddLayer(new SpriteSpecifier.Rsi(uid.Comp.RsiPath, uid.Comp.States));
        //todo StarMarkOffsetComp for doop's anomalocarids
        //would also let like, monkeys & such get the mark as well maybe
        sprite.LayerMapSet(CosmicRevealedKey.Key, layer);
        sprite.LayerSetShader(layer, "unshaded");
    }
    private void OnCosmicImpositionAdded(Entity<CosmicImposingComponent> uid, ref ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite) || sprite.LayerMapTryGet(CosmicImposingKey.Key, out _))
            return;

        var layer = sprite.AddLayer(new SpriteSpecifier.Rsi(uid.Comp.RsiPath, uid.Comp.States));

        sprite.LayerMapSet(CosmicImposingKey.Key, layer);
        sprite.LayerSetShader(layer, "unshaded");
    }
    #endregion

    #region Removals
    private void OnAscendedInfectionRemoved(Entity<RogueAscendedInfectionComponent> uid, ref ComponentShutdown args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite) || !sprite.LayerMapTryGet(AscendedInfectionKey.Key, out var layer))
            return;

        sprite.RemoveLayer(layer);
    }
    private void OnAscendedAuraRemoved(Entity<RogueAscendedAuraComponent> uid, ref ComponentShutdown args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite) || !sprite.LayerMapTryGet(AscendedAuraKey.Key, out var layer))
            return;

        sprite.RemoveLayer(layer);
    }
    private void OnCosmicStarMarkRemoved(Entity<CosmicStarMarkComponent> uid, ref ComponentShutdown args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite) || !sprite.LayerMapTryGet(CosmicRevealedKey.Key, out var layer))
            return;

        sprite.RemoveLayer(layer);
    }
    private void OnCosmicImpositionRemoved(Entity<CosmicImposingComponent> uid, ref ComponentShutdown args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite) || !sprite.LayerMapTryGet(CosmicImposingKey.Key, out var layer))
            return;

        sprite.RemoveLayer(layer);
    }
    #endregion

    #region Icons
    private void GetCosmicCultIcon(Entity<CosmicCultComponent> ent, ref GetStatusIconsEvent args)
    {
        if (HasComp<CosmicCultLeadComponent>(ent))
            return;

        if (_prototype.TryIndex(ent.Comp.StatusIcon, out var iconPrototype))
            args.StatusIcons.Add(iconPrototype);
    }

    private void GetCosmicCultLeadIcon(Entity<CosmicCultLeadComponent> ent, ref GetStatusIconsEvent args)
    {
        if (_prototype.TryIndex(ent.Comp.StatusIcon, out var iconPrototype))
            args.StatusIcons.Add(iconPrototype);
    }

    private void GetCosmicSSDIcon(Entity<CosmicMarkBlankComponent> ent, ref GetStatusIconsEvent args)
    {
        if (_prototype.TryIndex(ent.Comp.StatusIcon, out var iconPrototype))
            args.StatusIcons.Add(iconPrototype);
    }
    #endregion
}
public enum CultSiphonedVisuals : byte
{
    Key
}
