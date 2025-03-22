using Content.Client.Crayon.Overlays;
using Content.Client.Items;
using Content.Client.Message;
using Content.Client.Stylesheets;
using Content.Shared.Crayon;
using Content.Shared.Decals;
using Content.Shared.GameTicking;
using Content.Shared.Interaction;
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
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    // Didn't do in shared because I don't think most of the server stuff can be predicted.
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CrayonComponent, ComponentHandleState>(OnCrayonHandleState);
        Subs.ItemStatus<CrayonComponent>(ent => new StatusControl(ent));

        SubscribeLocalEvent<CrayonComponent, CrayonSelectMessage>(OnCrayonSelectMessage);
        SubscribeLocalEvent<CrayonComponent, CrayonColorMessage>(OnCrayonColorMessage);
        SubscribeLocalEvent<CrayonComponent, CrayonRotationMessage>(OnCrayonRotationMessage);
        SubscribeNetworkEvent<CrayonOverlayUpdateEvent>(OnCrayonOverlayUpdate);

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
                ("color",_parent.Color),
                ("state",_parent.SelectedState),
                ("charges", _parent.Charges),
                ("capacity",_parent.Capacity)));
        }
    }

    private DecalPrototype? GetDecal(string decalId)
    {
        return decalId != null ?
            _protoMan.Index<DecalPrototype>(decalId) : null;
    }

    private void UpdateOverlayInternal(string state, float rotation, Color color, bool previewMode)
    {
        _overlay.RemoveOverlay<CrayonDecalPlacementOverlay>();

        if (previewMode)
        {
            _overlay.AddOverlay(new CrayonDecalPlacementOverlay(_transform, _sprite, _interaction, GetDecal(state), Angle.FromDegrees(rotation), color));
        }
    }

    private void OnCrayonOverlayUpdate(CrayonOverlayUpdateEvent args)
    {
        UpdateOverlayInternal(args.State, args.Rotation, args.Color, args.PreviewMode);
    }

    private void OnCrayonSelectMessage(EntityUid uid, CrayonComponent component, ref CrayonSelectMessage args)
    {
        UpdateOverlayInternal(args.State, component.Rotation, component.Color, component.PreviewMode);
    }

    private void OnCrayonColorMessage(EntityUid uid, CrayonComponent component, ref CrayonColorMessage args)
    {
        UpdateOverlayInternal(component.State, component.Rotation, args.Color, component.PreviewMode);
    }

    private void OnCrayonRotationMessage(EntityUid uid, CrayonComponent component, ref CrayonRotationMessage args)
    {
        UpdateOverlayInternal(component.State, args.Rotation, component.Color, component.PreviewMode);
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
