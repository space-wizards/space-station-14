using Content.Client.Crayon.Overlays;
using Content.Client.Decals;
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
    [Dependency] private readonly DecalPlacementSystem _placement = default!;

    // Didn't do in shared because I don't think most of the server stuff can be predicted.
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CrayonComponent, AfterAutoHandleStateEvent>(OnAfterHandleState);
        Subs.ItemStatus<CrayonComponent>(ent => new StatusControl(ent));

        SubscribeLocalEvent<LocalPlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        SubscribeLocalEvent<CrayonComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnAfterHandleState(Entity<CrayonComponent> crayon, ref AfterAutoHandleStateEvent args)
    {
        crayon.Comp.UIUpdateNeeded = true;
        UpdateOverlayInternal(crayon.Comp.State, crayon.Comp.Rotation, crayon.Comp.Color, crayon.Comp.PreviewMode);
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

            _parent.UIUpdateNeeded = true;
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
                ("state", _parent.State),
                ("charges", _parent.Charges),
                ("capacity", _parent.Capacity),
                ("rotation", _parent.Rotation)));
        }
    }

    private DecalPrototype? GetDecal(ProtoId<DecalPrototype>? decalId)
    {
        return decalId is { } id ? _protoMan.Index(id) : null;
    }

    private void UpdateOverlayInternal(ProtoId<DecalPrototype>? state, float rotation, Color color, bool previewMode)
    {
        _overlay.RemoveOverlay<CrayonDecalPlacementOverlay>();

        if (previewMode)
        {
            _overlay.AddOverlay(new CrayonDecalPlacementOverlay(_placement, _transform, _sprite, _interaction, GetDecal(state), Angle.FromDegrees(rotation), color));
        }
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
