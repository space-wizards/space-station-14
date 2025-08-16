using Content.Client.Crayon.Overlays;
using Content.Client.Decals;
using Content.Client.Items;
using Content.Client.Message;
using Content.Client.Stylesheets;
using Content.Shared.Crayon;
using Content.Shared.Decals;
using Content.Shared.GameTicking;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameStates;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.Crayon;

public sealed class CrayonSystem : SharedCrayonSystem
{
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly DecalPlacementSystem _placement = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    // Didn't do in shared because I don't think most of the server stuff can be predicted.
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CrayonComponent, ComponentHandleState>(OnCrayonHandleState);
        Subs.ItemStatus<CrayonComponent>(ent => new StatusControl(ent));

        SubscribeLocalEvent<LocalPlayerDetachedEvent>(OnLocalPlayerDetached);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
        SubscribeLocalEvent<CrayonComponent, HandDeselectedEvent>(OnHandDeselected);
        SubscribeLocalEvent<CrayonComponent, GotUnequippedHandEvent>(OnUnequip);
        SubscribeLocalEvent<CrayonComponent, ComponentShutdown>(OnComponentShutdown);
    }

    private void OnCrayonHandleState(EntityUid uid, CrayonComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not CrayonComponentState state) return;

        UpdateOverlay(uid, component, state.State, state.Rotation, state.Color, state.PreviewEnabled, state.PreviewVisible, state.OpaqueGhost);

        component.Color = state.Color;
        component.SelectedState = state.State;
        component.Charges = state.Charges;
        component.Capacity = state.Capacity;
        component.Rotation = state.Rotation;

        component.UIUpdateNeeded = true;
    }

    private sealed class StatusControl : Control
    {
        private readonly CrayonComponent _parent;
        private readonly RichTextLabel _label;

        public StatusControl(CrayonComponent parent)
        {
            _parent = parent;
            _label = new RichTextLabel { StyleClasses = { StyleNano.StyleClassItemStatus } };
            AddChild(_label);

            parent.UIUpdateNeeded = true;
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);

            if (!_parent.UIUpdateNeeded)
            {
                return;
            }

            _parent.UIUpdateNeeded = false;
            _label.SetMarkup(Robust.Shared.Localization.Loc.GetString("crayon-drawing-label",
                ("color",_parent.Color),
                ("rotation",_parent.Rotation),
                ("previewEnabled",_parent.PreviewEnabled),
                ("previewVisible",_parent.PreviewVisible),
                ("state",_parent.SelectedState),
                ("charges", _parent.Charges),
                ("capacity",_parent.Capacity)));
        }
    }

    private void UpdateOverlay(EntityUid uid, CrayonComponent component, ProtoId<DecalPrototype>? state, float rotation, Color color, bool preview, bool previewVisible, bool opaqueGhost)
    {
        if (_player.LocalEntity == null || _handsSystem.GetActiveItem(_player.LocalEntity.Value) != uid)
            return;
        _overlay.RemoveOverlay<CrayonDecalGhostOverlay>();
        if (!preview||!previewVisible)
            return;
        var decal = state is { } id ? _prototypeManager.Index(id) : null;
        if (opaqueGhost)
            color.A = 0.5f;
        _overlay.AddOverlay(new CrayonDecalGhostOverlay(_placement, _transform, _sprite, _interaction, decal, -rotation, color));
    }

    private void OnLocalPlayerDetached(LocalPlayerDetachedEvent args)
    {
        RemoveOverlay();
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent args)
    {
        RemoveOverlay();
    }

    private void OnHandDeselected(EntityUid uid, CrayonComponent component, ref HandDeselectedEvent args)
    {
        RemoveOverlay();
    }

    private void OnUnequip(EntityUid uid, CrayonComponent component, ref GotUnequippedHandEvent args)
    {
        if(args.Unequipped==uid) RemoveOverlay();
    }

    private void OnComponentShutdown(EntityUid uid, CrayonComponent component, ComponentShutdown args)
    {
        RemoveOverlay();
    }

    private void RemoveOverlay()
    {
        _overlay.RemoveOverlay<CrayonDecalGhostOverlay>();
    }
}
