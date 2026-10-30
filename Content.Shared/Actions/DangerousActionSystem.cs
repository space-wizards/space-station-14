using Content.Shared.Actions.Components;
using Content.Shared.Actions.Events;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Popups;

namespace Content.Shared.Actions;

public sealed partial class DangerousActionSystem : EntitySystem
{
    [Dependency] private EntityQuery<PacifiedComponent> _pacifiedQuery = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DangerousActionComponent, ActionAttemptEvent>(OnAttempt);
    }

    private void OnAttempt(Entity<DangerousActionComponent> ent, ref ActionAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!_pacifiedQuery.HasComp(args.User))
            return;

        args.Reason = Loc.GetString(ent.Comp.PacificationMessage);
        args.Type = ent.Comp.MessageType;

        args.Cancelled = true;
    }

}
