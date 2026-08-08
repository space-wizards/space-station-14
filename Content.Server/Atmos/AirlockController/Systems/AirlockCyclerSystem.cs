using Content.Server.Atmos.AirlockController.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Atmos.AirlockController;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Robust.Shared.Timing;

namespace Content.Server.Atmos.AirlockController.Systems;

/// <summary>
///     Panels are dumb devices. The controller writes their state, and they send back requests to cycle
/// </summary>
public sealed partial class AirlockCyclerSystem : EntitySystem
{
    [Dependency] private AirlockControllerSystem _controller = default!;
    [Dependency] private AirlockCycleStatusSystem _status = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private IGameTiming _timing = default!;

    private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

    private TimeSpan _nextUpdate;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AirlockCyclerComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<AirlockCyclerComponent, ExaminedEvent>(OnExamine);

        Subs.BuiEvents<AirlockCyclerComponent>(AirlockControllerUiKey.Key,
            subs =>
        {
            subs.Event<AirlockCyclerCycleMessage>(OnCycleMessage);
        });
    }

    public override void Update(float frameTime)
    {
        if (_timing.CurTime < _nextUpdate)
            return;

        _nextUpdate = _timing.CurTime + UpdateInterval;

        var query = EntityQueryEnumerator<AirlockCyclerComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            // A controller that blew up stops claiming its panels
            if (comp.Controller is { } controller && TerminatingOrDeleted(controller))
                comp.Controller = null;

            // Bound panels are written by their controller
            if (comp.Controller == null)
            {
                comp.ChamberPressure = null;
                comp.Status = Unbound(comp.Side);
                _status.Apply(uid, comp.Status);
            }

            if (_ui.IsUiOpen(uid, AirlockControllerUiKey.Key))
                UpdateUi((uid, comp));
        }
    }

    /// <summary>
    ///     Shows as uninstalled controller
    /// </summary>
    private static AirlockCycleStatus Unbound(AirlockSide side)
    {
        return new AirlockCycleStatus(AirlockCycleState.Idle, side, null, true, false);
    }

    private void OnActivate(Entity<AirlockCyclerComponent> ent, ref ActivateInWorldEvent args)
    {
        if (!args.Complex || args.Handled || !this.IsPowered(ent, EntityManager))
            return;

        _ui.OpenUi(ent.Owner, AirlockControllerUiKey.Key, args.User);
        UpdateUi(ent);
        args.Handled = true;
    }

    private void OnCycleMessage(Entity<AirlockCyclerComponent> ent, ref AirlockCyclerCycleMessage args)
    {
        if (ent.Comp.Controller is { } controller && TryComp<AirlockControllerComponent>(controller, out var comp))
            _controller.TryRequestCycleFrom((controller, comp), ent.Owner, args.Actor);

        UpdateUi(ent);
    }

    private void OnExamine(Entity<AirlockCyclerComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (ent.Comp.Controller == null)
        {
            args.PushMarkup(Loc.GetString("airlock-cycler-examine-unbound"),
                AirlockCycleStatusSystem.StatusPriority);
            return;
        }

        _status.Examine(ent.Comp.Status, args);
    }

    private void UpdateUi(Entity<AirlockCyclerComponent> ent)
    {
        var comp = ent.Comp;

        _ui.SetUiState(ent.Owner, AirlockControllerUiKey.Key, new AirlockCyclerUiState
        {
            Bound = comp.Controller != null,
            Side = comp.Side,
            Status = comp.Status,
            ChamberPressure = comp.ChamberPressure,
        });
    }
}
