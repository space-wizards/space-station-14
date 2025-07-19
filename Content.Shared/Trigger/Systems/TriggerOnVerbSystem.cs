using Content.Shared.Verbs;
using Content.Shared.Trigger.Components.Triggers;

namespace Content.Shared.Trigger.Systems;

public sealed partial class TriggerOnVerbSystem : EntitySystem
{
    [Dependency] private readonly TriggerSystem _trigger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TriggerOnVerbComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAltVerbs);
    }

    private void OnGetAltVerbs(Entity<TriggerOnVerbComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess || args.Hands == null)
            return;

        var user = args.User;

        args.Verbs.Add(new AlternativeVerb()
        {
            Text = Loc.GetString(ent.Comp.Text),
            Act = () => DoTrigger(ent, user),
            Priority = 2 // should be above any timer settings
        });
    }

    private void DoTrigger(Entity<TriggerOnVerbComponent> ent, EntityUid user)
    {
        _trigger.Trigger(ent.Owner, user, ent.Comp.KeyOut);
    }
}
