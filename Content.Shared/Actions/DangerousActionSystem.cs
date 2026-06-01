using Content.Shared.Actions.Components;
using Content.Shared.Actions.Events;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Popups;

namespace Content.Shared.Actions;
/// <summary>
/// Just the system to realize Dangerous Action Component.
/// <seealso cref="DangerousActionComponent"/>
/// </summary>
public sealed partial class DangerousActionSystem : EntitySystem
{
    [Dependency] private SharedPopupSystem _popup = default!;
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
        //query for pacification
        if (!_pacifiedQuery.HasComp(args.User))
            return;
        //if found popup message and cancel
        _popup.PopupClient(Loc.GetString(ent.Comp.PacificationMessage),args.User,args.User,PopupType.Small);
        args.Cancelled = true;
    }

}
