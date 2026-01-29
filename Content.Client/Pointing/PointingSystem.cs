using Content.Client.Pointing.Components;
using Content.Shared.Pointing;
using Content.Shared.Verbs;
using Robust.Client.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;
using Robust.Shared.Configuration;
using Content.Shared.CCVar;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client.Pointing;

public sealed partial class PointingSystem : SharedPointingSystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GetVerbsEvent<Verb>>(AddPointingVerb);
        SubscribeLocalEvent<PointingArrowComponent, ComponentStartup>(OnArrowStartup);
        SubscribeLocalEvent<RoguePointingArrowComponent, ComponentStartup>(OnRogueArrowStartup);
        SubscribeLocalEvent<PointingArrowComponent, ComponentHandleState>(HandleCompState);

        // Subscribe to CVar changes for real-time updates
        _cfg.OnValueChanged(CCVars.PointerHighlight, _ => UpdateAllPointers());
        _cfg.OnValueChanged(CCVars.ChatHighlightsColor, _ => UpdateAllPointers());

        InitializeVisualizer();
    }

    private void UpdateAllPointers()
    {
        var query = EntityQueryEnumerator<PointingArrowComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out _, out var sprite))
        {
            UpdatePointerAppearance(uid, sprite);
        }
    }

    private void UpdatePointerAppearance(Entity<SpriteComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp))
            return;

        var useHighlight = _cfg.GetCVar(CCVars.PointerHighlight);

        if (useHighlight)
        {
            // Use blank sprite and apply highlight color
            sprite.LayerSetState(0, "pointing_blank");
            var highlightColor = Color.FromHex(_cfg.GetCVar(CCVars.ChatHighlightsColor));
            sprite.Color = highlightColor;
        }
        else
        {
            // Use default pointing sprite with configured or white color
            sprite.LayerSetState(0, "pointing");
            var color = Color.FromHex(_cfg.GetCVar(CCVars.PointingArrowColor));
            sprite.Color = color;
        }
    }

    private void AddPointingVerb(GetVerbsEvent<Verb> args)
    {
        if (IsClientSide(args.Target))
            return;

        // Really this could probably be a properly predicted event, but that requires reworking pointing. For now
        // I'm just adding this verb exclusively to clients so that the verb-loading pop-in on the verb menu isn't
        // as bad. Important for this verb seeing as its usually an option on just about any entity.

        // this is a pointing arrow. no pointing here...
        if (HasComp<PointingArrowComponent>(args.Target))
            return;

        if (!CanPoint(args.User))
            return;

        // We won't check in range or visibility, as this verb is currently only executable via the context menu,
        // and that should already have checked that, as well as handling the FOV-toggle stuff.

        Verb verb = new()
        {
            Text = Loc.GetString("pointing-verb-get-data-text"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/point.svg.192dpi.png")),
            ClientExclusive = true,
            Act = () => RaiseNetworkEvent(new PointingAttemptEvent(GetNetEntity(args.Target)))
        };

        args.Verbs.Add(verb);
    }

    private void OnArrowStartup(EntityUid uid, PointingArrowComponent component, ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        _sprite.SetDrawDepth((uid, sprite), (int)DrawDepth.Overlays);

        // Apply initial appearance
        UpdatePointerAppearance(uid, sprite);

        BeginPointAnimation(uid, component.StartPosition, component.Offset, component.AnimationKey);
    }

    private void OnRogueArrowStartup(EntityUid uid, RoguePointingArrowComponent arrow, ComponentStartup args)
    {
        if (TryComp<SpriteComponent>(uid, out var sprite))
        {
            _sprite.SetDrawDepth((uid, sprite), (int)DrawDepth.Overlays);
            sprite.NoRotation = false;
        }
    }

    private void HandleCompState(Entity<PointingArrowComponent> entity, ref ComponentHandleState args)
    {
        if (args.Current is not SharedPointingArrowComponentState state)
            return;

        entity.Comp.StartPosition = state.StartPosition;
        entity.Comp.EndTime = state.EndTime;
    }
}
