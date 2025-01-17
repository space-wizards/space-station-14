using Content.Client.Crayon.Overlays;
using Content.Client.Items;
using Content.Client.Message;
using Content.Client.Stylesheets;
using Content.Shared.Crayon;
using Content.Shared.Decals;
using Content.Shared.GameTicking;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Inventory.Events;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameStates;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.Crayon;

public sealed class CrayonSystem : SharedCrayonSystem
{
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    // Didn't do in shared because I don't think most of the server stuff can be predicted.
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CrayonComponent, ComponentHandleState>(OnCrayonHandleState);
        Subs.ItemStatus<CrayonComponent>(ent => new StatusControl(ent));

        SubscribeLocalEvent<CrayonComponent, CrayonSelectMessage>(OnCrayonSelectMessage);
        SubscribeLocalEvent<CrayonComponent, CrayonColorMessage>(OnCrayonColorMessage);
        SubscribeLocalEvent<CrayonComponent, CrayonRotationMessage>(OnCrayonRotationMessage);
        SubscribeLocalEvent<CrayonComponent, CrayonPreviewModeMessage>(OnCrayonPreviewModeMessage);

        SubscribeLocalEvent<CrayonComponent, BoundUIClosedEvent>(OnBuiClosed);
        SubscribeLocalEvent<CrayonComponent, HandDeselectedEvent>(OnHandDeselected);
        SubscribeLocalEvent<CrayonComponent, GotUnequippedEvent>(OnGotUnequipped);

        SubscribeLocalEvent<LocalPlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        SubscribeLocalEvent<CrayonComponent, ComponentShutdown>(OnShutdown);
    }

    public override void Shutdown()
    {
        base.Shutdown();
    }

    private static void OnCrayonHandleState(EntityUid uid, CrayonComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not CrayonComponentState state) return;

        component.Color = state.Color;
        component.SelectedState = state.State;
        component.Charges = state.Charges;
        component.Capacity = state.Capacity;
        component.State = state.State;
        component.SelectableColor = state.SelectableColor;
        component.Rotation = state.Rotation;
        component.PreviewMode = state.PreviewMode;

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
                ("color", _parent.Color),
                ("state", _parent.SelectedState),
                ("charges", _parent.Charges),
                ("capacity", _parent.Capacity)));
        }
    }

    private DecalPrototype? GetDecal(string decalId)
    {
        return decalId != null ?
            _protoMan.Index<DecalPrototype>(decalId) : null;
    }

    private void OnCrayonSelectMessage(EntityUid uid, CrayonComponent component, ref CrayonSelectMessage args)
    {
        if (component.PreviewMode)
        {
            _overlay.RemoveOverlay<CrayonDecalPlacementOverlay>();
            _overlay.AddOverlay(new CrayonDecalPlacementOverlay(_transform, _sprite, _interaction, GetDecal(args.State), Angle.FromDegrees(component.Rotation), component.Color));
        }
    }

    private void OnCrayonColorMessage(EntityUid uid, CrayonComponent component, ref CrayonColorMessage args)
    {
        if (component.PreviewMode)
        {
            _overlay.RemoveOverlay<CrayonDecalPlacementOverlay>();
            _overlay.AddOverlay(new CrayonDecalPlacementOverlay(_transform, _sprite, _interaction, GetDecal(component.State), Angle.FromDegrees(component.Rotation), args.Color));
        }
    }

    private void OnCrayonRotationMessage(EntityUid uid, CrayonComponent component, ref CrayonRotationMessage args)
    {
        if (component.PreviewMode)
        {
            _overlay.RemoveOverlay<CrayonDecalPlacementOverlay>();
            _overlay.AddOverlay(new CrayonDecalPlacementOverlay(_transform, _sprite, _interaction, GetDecal(component.State), Angle.FromDegrees(args.Rotation), component.Color));
        }
    }

    private void OnCrayonPreviewModeMessage(EntityUid uid, CrayonComponent component, ref CrayonPreviewModeMessage args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (!args.PreviewMode)
        {
            _overlay.RemoveOverlay<CrayonDecalPlacementOverlay>();
        }
        else if (TryComp<HandsComponent>(args.Actor, out var hands) &&
            TryComp<CrayonComponent>(hands.ActiveHandEntity, out var crayon) &&
            hands.ActiveHandEntity == uid)
        {
            // Only enable the overlay if the user is holding a crayon in their active hand
            // and check if it is the same crayon that sent the request
            _overlay.AddOverlay(new CrayonDecalPlacementOverlay(_transform, _sprite, _interaction, GetDecal(component.State), Angle.FromDegrees(component.Rotation), component.Color));
        }
        else
        {
            // failed to enable, untoggle button
            _ui.SetUiState(uid, SharedCrayonComponent.CrayonUiKey.Key, new CrayonBoundUserInterfaceState(component.SelectedState, component.SelectableColor, component.Color, component.Rotation, component.PreviewMode));
        }
    }

    private void OnBuiClosed(EntityUid uid, CrayonComponent component, BoundUIClosedEvent args)
    {
        component.PreviewMode = false;
        _overlay.RemoveOverlay<CrayonDecalPlacementOverlay>();
        _ui.SetUiState(uid, SharedCrayonComponent.CrayonUiKey.Key, new CrayonBoundUserInterfaceState(component.SelectedState, component.SelectableColor, component.Color, component.Rotation, component.PreviewMode));
    }

    private void OnHandDeselected(EntityUid uid, CrayonComponent component, ref HandDeselectedEvent args)
    {
        component.PreviewMode = false;
        _overlay.RemoveOverlay<CrayonDecalPlacementOverlay>();
        _ui.SetUiState(uid, SharedCrayonComponent.CrayonUiKey.Key, new CrayonBoundUserInterfaceState(component.SelectedState, component.SelectableColor, component.Color, component.Rotation, component.PreviewMode));
    }

    private void OnGotUnequipped(EntityUid uid, CrayonComponent component, ref GotUnequippedEvent args)
    {
        component.PreviewMode = false;
        _overlay.RemoveOverlay<CrayonDecalPlacementOverlay>();
        _ui.SetUiState(uid, SharedCrayonComponent.CrayonUiKey.Key, new CrayonBoundUserInterfaceState(component.SelectedState, component.SelectableColor, component.Color, component.Rotation, component.PreviewMode));
    }

    private void OnPlayerDetached(LocalPlayerDetachedEvent args)
    {
        _overlay.RemoveOverlay<CrayonDecalPlacementOverlay>();
    }

    private void OnRoundRestart(RoundRestartCleanupEvent args)
    {
        _overlay.RemoveOverlay<CrayonDecalPlacementOverlay>();
    }

    private void OnShutdown(EntityUid uid, CrayonComponent component, ref ComponentShutdown args)
    {
        _overlay.RemoveOverlay<CrayonDecalPlacementOverlay>();
    }
}
