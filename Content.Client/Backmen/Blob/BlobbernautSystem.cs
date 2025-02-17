using System.Linq;
using Content.Client.DamageState;
using Content.Shared.Backmen.Blob;
using Content.Shared.Backmen.Blob.Components;
using Content.Shared.Damage;
using Content.Shared.Popups;
using Robust.Client.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Client.Backmen.Blob;

public sealed class BlobbernautSystem : SharedBlobbernautSystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    protected override DamageSpecifier? TryChangeDamage(string msg, EntityUid ent, DamageSpecifier dmg)
    {
        _popup.PopupClient(Loc.GetString(msg), ent, ent, PopupType.LargeCaution);
        return null;
    }
}

public sealed class BlobbernautVisualizerSystem : VisualizerSystem<BlobbernautComponent>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BlobbernautComponent, AfterAutoHandleStateEvent>(OnBlobTileHandleState);
    }

    private void UpdateAppearance(EntityUid id, BlobbernautComponent blobbernaut, AppearanceComponent? appearance = null, SpriteComponent? sprite = null)
    {
        if (!Resolve(id, ref appearance, ref sprite))
            return;

        foreach (var key in new []{ DamageStateVisualLayers.Base, DamageStateVisualLayers.BaseUnshaded })
        {
            if (!sprite.LayerMapTryGet(key, out _))
                continue;

            sprite.LayerSetColor(key, blobbernaut.Color);
        }
    }

    protected override void OnAppearanceChange(EntityUid uid, BlobbernautComponent component, ref AppearanceChangeEvent args)
    {
        UpdateAppearance(uid, component, args.Component, args.Sprite);
    }

    private void OnBlobTileHandleState(EntityUid uid, BlobbernautComponent component, ref AfterAutoHandleStateEvent args)
    {
        UpdateAppearance(uid, component);
    }
}
