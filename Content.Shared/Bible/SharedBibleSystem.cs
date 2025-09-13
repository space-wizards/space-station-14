using Content.Shared.ActionBlocker;
using Content.Shared.Bible.Components;
using Content.Shared.Verbs;

namespace Content.Shared.Bible;

/// <summary>
/// Shared bible system basically for GetVerbsEvent predictions.
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

    /// <summary>
    /// Check is entity summonable.
    /// </summary>
    /// <returns></returns>
    private bool CheckSummonable(Entity<SummonableComponent> ent, EntityUid user, TransformComponent position)
    {
        var uid = ent.Owner;
        var component = ent.Comp;

        if (component.AlreadySummoned || component.SpecialItemPrototype == null)
            return false;
        if (component.RequiresBibleUser && !HasComp<BibleUserComponent>(user))
            return false;
        if (component.Deleted || Deleted(uid))
            return false;
        if (!_blocker.CanInteract(user, uid))
            return false;

        return true;
    }

    /// <summary>
    /// Try to summon with checks.
    /// </summary>
    protected void AttemptSummon(Entity<SummonableComponent> ent, EntityUid user, TransformComponent? position)
    {
        // TODO : because it's predicted, maybe put some popup feedback to the player/client?
        if (!Resolve(user, ref position) || !CheckSummonable(ent, user, position))
        {
            // _popup.PopupClient failure
            return;
        }

        Summon(ent, user, position);
    }

    /// <summary>
    /// Internal server's side entity summoning.
    /// </summary>
    /// <remarks>
    /// Only activated on Shared side when <see cref="SharedBibleSystem.CheckSummonable"/> passed.
    /// </remarks>
    protected virtual void Summon(Entity<SummonableComponent> ent, EntityUid user, TransformComponent position)
    {
        // Server-side logic.
    }
}
