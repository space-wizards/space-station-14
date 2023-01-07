using Content.Shared.ActionBlocker;
using Content.Shared.Administration.Logs;
using Content.Shared.Alert;
using Content.Shared.Mobs.Components;
using Content.Shared.Standing;
using Content.Shared.StatusEffect;
using Robust.Shared.GameStates;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.Mobs.Systems;

[Virtual]
public partial class MobStateSystem : EntitySystem
{
    [Dependency] protected readonly AlertsSystem Alerts = default!;
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] protected readonly StatusEffectsSystem Status = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeMiscEvents();
        SubscribeLocalEvent<MobStateComponent, ComponentShutdown>(OnMobStateShutdown);
        SubscribeLocalEvent<MobStateComponent, ComponentGetState>(OnGetComponentState);
        SubscribeLocalEvent<MobStateComponent, ComponentHandleState>(OnHandleComponentState);
    }


    private void OnHandleComponentState(EntityUid uid, MobStateComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not MobStateComponentState state)
            return;
        component.AllowedStates = state.AllowedStates;
        component.StateTickets = state.StateTickets;
    }

    private void OnGetComponentState(EntityUid uid, MobStateComponent component, ref ComponentGetState args)
    {
        args.State = new MobStateComponentState(component.AllowedStates, component.StateTickets);
    }
    private void OnMobStateShutdown(EntityUid uid, MobStateComponent component, ComponentShutdown args)
    {
        Alerts.ClearAlert(uid, AlertType.HumanHealth);
    }

    public bool HasState(EntityUid uid, MobState mobState, MobStateComponent? component = null)
    {
        return Resolve(uid, ref component, false) && component.AllowedStates.Contains(mobState);
    }

    public bool IsAlive(EntityUid uid, MobStateComponent? component = null)
    {
        if (!Resolve(uid, ref component, false)) return false;
        return component.CurrentState == MobState.Alive;
    }

    public bool IsCritical(EntityUid uid, MobStateComponent? component = null)
    {
        if (!Resolve(uid, ref component, false)) return false;
        return component.CurrentState == MobState.Critical;
    }

    public bool IsDead(EntityUid uid, MobStateComponent? component = null)
    {
        if (!Resolve(uid, ref component, false)) return false;
        return component.CurrentState == MobState.Dead;
    }

    public bool IsIncapacitated(EntityUid uid, MobStateComponent? component = null)
    {
        if (!Resolve(uid, ref component, false)) return false;
        return component.CurrentState is MobState.Critical or MobState.Dead;
    }

}
