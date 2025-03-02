using Content.Shared._Impstation.CosmicCult.Components;
using Content.Shared._Impstation.CosmicCult;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;
using Content.Shared._Impstation.CosmicCult.Components.Examine;

namespace Content.Client._Impstation.CosmicCult;

public sealed partial class CosmicCultSystem : SharedCosmicCultSystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CosmicStarMarkComponent, ComponentStartup>(OnCosmicStarMarkAdded);
        SubscribeLocalEvent<CosmicStarMarkComponent, ComponentShutdown>(OnCosmicStarMarkRemoved);

        SubscribeLocalEvent<CosmicImposingComponent, ComponentStartup>(OnCosmicImpositionAdded);
        SubscribeLocalEvent<CosmicImposingComponent, ComponentShutdown>(OnCosmicImpositionRemoved);

        SubscribeLocalEvent<CosmicCultComponent, GetStatusIconsEvent>(GetCosmicCultIcon);
        SubscribeLocalEvent<CosmicCultLeadComponent, GetStatusIconsEvent>(GetCosmicCultLeadIcon);
        SubscribeLocalEvent<CosmicMarkBlankComponent, GetStatusIconsEvent>(GetCosmicSSDIcon);
    }

    private void OnCosmicStarMarkAdded(Entity<CosmicStarMarkComponent> uid, ref ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite) || sprite.LayerMapTryGet(CosmicRevealedKey.Key, out _))
            return;

        var layer = sprite.AddLayer(new SpriteSpecifier.Rsi(uid.Comp.RsiPath, uid.Comp.States));

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
}
