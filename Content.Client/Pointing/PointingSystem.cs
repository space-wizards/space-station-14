using Content.Client.Pointing.Components;
using Content.Client.Gravity;
using Content.Shared.Mobs.Systems;
using Content.Shared.Pointing;
using Content.Shared.Verbs;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client.Pointing;

public sealed class PointingSystem : SharedPointingSystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly FloatingVisualizerSystem _floatingSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GetVerbsEvent<Verb>>(AddPointingVerb);
        SubscribeLocalEvent<PointingArrowComponent, ComponentStartup>(OnArrowStartup);
        SubscribeLocalEvent<PointingArrowComponent, AnimationCompletedEvent>(OnArrowAnimation);
        SubscribeLocalEvent<RoguePointingArrowComponent, ComponentStartup>(OnRogueArrowStartup);
    }

    private void OnArrowAnimation(EntityUid uid, PointingArrowComponent component, AnimationCompletedEvent args)
    {
        _floatingSystem.FloatAnimation(uid, component.Offset, component.AnimationKey, component.AnimationTime);
    }

    private void AddPointingVerb(GetVerbsEvent<Verb> args)
    {
        if (IsClientSide(args.Target))
            return;

        // Really this could probably be a properly predicted event, but that requires reworking pointing. For now
        // I'm just adding this verb exclusively to clients so that the verb-loading pop-in on the verb menu isn't
        // as bad. Important for this verb seeing as its usually an option on just about any entity.

        if (HasComp<PointingArrowComponent>(args.Target))
        {
            // this is a pointing arrow. no pointing here...
            return;
        }

        // Can the user point? Checking mob state directly instead of some action blocker, as many action blockers are blocked for
        // ghosts and there is no obvious choice for pointing (unless ghosts CanEmote?).
        if (_mobState.IsIncapacitated(args.User))
            return;

        // We won't check in range or visibility, as this verb is currently only executable via the context menu,
        // and that should already have checked that, as well as handling the FOV-toggle stuff.

        Verb verb = new()
        {
            Text = Loc.GetString("pointing-verb-get-data-text"),
            Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/VerbIcons/point.svg.192dpi.png")),
            ClientExclusive = true,
            Act = () => RaiseNetworkEvent(new PointingAttemptEvent(GetNetEntity(args.Target)))
        };

        args.Verbs.Add(verb);
    }

    private void OnArrowStartup(EntityUid uid, PointingArrowComponent component, ComponentStartup args)
    {
        if (TryComp<SpriteComponent>(uid, out var sprite))
        {
            sprite.DrawDepth = (int) DrawDepth.Overlays;
        }

        _floatingSystem.FloatAnimation(uid, component.Offset, component.AnimationKey, component.AnimationTime);
    }

    private void OnRogueArrowStartup(EntityUid uid, RoguePointingArrowComponent arrow, ComponentStartup args)
    {
        if (TryComp<SpriteComponent>(uid, out var sprite))
        {
            sprite.DrawDepth = (int) DrawDepth.Overlays;
            sprite.NoRotation = false;
        }
    }
}
