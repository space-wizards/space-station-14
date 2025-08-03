using Content.Shared.ActionBlocker;
using Content.Shared.Bible.Components;
using Content.Shared.Verbs;

namespace Content.Shared.Bible;

/// <summary>
/// Shared bible system.
/// </summary>
public abstract class SharedBibleSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SummonableComponent, GetVerbsEvent<AlternativeVerb>>(AddSummonVerb);
    }

    /// <summary>
    /// Handles verb display for summoning, so the verb is predicted client-side.
    /// </summary>
    private void AddSummonVerb(EntityUid uid, SummonableComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess || component.AlreadySummoned || component.SpecialItemPrototype == null)
            return;

        if (component.RequiresBibleUser && !HasComp<BibleUserComponent>(args.User))
            return;

        AlternativeVerb verb = new()
        {
            Act = () =>
            {
                if (!TryComp(args.User, out TransformComponent? userXform))
                    return;

                AttemptSummon((uid, component), args.User, userXform);
            },
            Text = Loc.GetString("bible-summon-verb"),
            Priority = 2
        };

        args.Verbs.Add(verb);
    }

    protected void AttemptSummon(Entity<SummonableComponent> ent, EntityUid user, TransformComponent? position)
    {
        // TODO : because it's predicted, maybe put some popup feedback to the player/client?
        var uid = ent.Owner;
        var component = ent.Comp;

        if (component.AlreadySummoned || component.SpecialItemPrototype == null)
            return;
        if (component.RequiresBibleUser && !HasComp<BibleUserComponent>(user))
            return;
        if (!Resolve(user, ref position))
            return;
        if (component.Deleted || Deleted(uid))
            return;
        if (!_blocker.CanInteract(user, uid))
            return;

        Summon(ent, user, position);
    }

    protected virtual void Summon(Entity<SummonableComponent> ent, EntityUid user, TransformComponent position)
    {
        // Server-side logic.
    }
}
