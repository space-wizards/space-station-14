using Content.Shared.Bible.Components;
using Content.Shared.Verbs;

namespace Content.Shared.Bible;

/// <summary>
/// Shared bible system.
/// </summary>
public abstract class SharedBibleSystem : EntitySystem
{
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

    protected virtual void AttemptSummon(Entity<SummonableComponent> ent, EntityUid user, TransformComponent? position)
    {
        // Server-side logic.
    }
}
