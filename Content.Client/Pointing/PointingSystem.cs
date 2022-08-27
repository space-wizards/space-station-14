using Content.Client.Pointing.Components;
using Content.Shared.MobState.EntitySystems;
using Content.Shared.Pointing;
using Content.Shared.Verbs;
using Robust.Client.GameObjects;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client.Pointing;

public sealed class PointingSystem : EntitySystem
{
    [Dependency] private readonly SharedMobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GetVerbsEvent<Verb>>(AddPointingVerb);
        SubscribeLocalEvent<PointingArrowComponent, ComponentStartup>(OnArrowStartup);
        SubscribeLocalEvent<RoguePointingArrowComponent, ComponentStartup>(OnRogueArrowStartup);

    }

    private void AddPointingVerb(GetVerbsEvent<Verb> args)
    {
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
            IconTexture = "/Textures/Interface/VerbIcons/point.svg.192dpi.png",
            ClientExclusive = true,
            Act = () => RaiseNetworkEvent(new PointingAttemptEvent(args.Target))
        };

        args.Verbs.Add(verb);
    }

    private void OnArrowStartup(EntityUid uid, PointingArrowComponent arrow, ComponentStartup args)
    {
        if (EntityManager.TryGetComponent(uid, out SpriteComponent? sprite))
        {
            sprite.DrawDepth = (int) DrawDepth.Overlays;
        }
    }

    private void OnRogueArrowStartup(EntityUid uid, RoguePointingArrowComponent arrow, ComponentStartup args)
    {
        if (EntityManager.TryGetComponent(uid, out SpriteComponent? sprite))
        {
            sprite.DrawDepth = (int) DrawDepth.Overlays;
        }
    }
}
