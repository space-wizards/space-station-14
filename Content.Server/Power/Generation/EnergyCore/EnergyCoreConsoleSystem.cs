using Content.Server.Power.Components;
using Content.Shared.DeviceLinking.Components;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.DeviceLinking.Systems;
using Content.Shared.Power.Generation.EnergyCore;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server.Power.Generation.EnergyCore;

public sealed partial class EnergyCoreConsoleSystem : EntitySystem
{
    [Dependency] private DeviceLinkSystem _links = default!;
    [Dependency] private EnergyCoreSystem _cores = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;
    [Dependency] private IGameTiming _timing = default!;

    private TimeSpan _nextUpdate;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EnergyCoreConsoleComponent, LinkAttemptEvent>(OnLinkAttemptEvent);
        SubscribeLocalEvent<EnergyCoreConsoleComponent, BoundUIOpenedEvent>(OnUiOpenedEvent);
        SubscribeLocalEvent<EnergyCoreConsoleComponent, EnergyCoreSetEnabledMessage>(OnSetEnabledMessage);
        SubscribeLocalEvent<EnergyCoreConsoleComponent, EnergyCoreSetOutputMessage>(OnSetOutputMessage);
    }

    private void OnLinkAttemptEvent(Entity<EnergyCoreConsoleComponent> ent, ref LinkAttemptEvent args)
    {
        if (args.SourcePort != ent.Comp.SourcePort)
            return;

        if (args.SinkPort != ent.Comp.SinkPort || !HasComp<EnergyCoreComponent>(args.Sink) ||
            TryGetCore((ent.Owner, ent.Comp), out var existing) && existing.Owner != args.Sink)
            args.Cancelled = true;
    }

    public bool TryGetCore(Entity<EnergyCoreConsoleComponent?> ent, out Entity<EnergyCoreComponent> core)
    {
        core = default;
        if (!Resolve(ent, ref ent.Comp, false) || !TryComp<DeviceLinkSourceComponent>(ent, out var source))
            return false;

        var found = false;
        foreach (var uid in _links.GetLinkedSinks((ent.Owner, source), ent.Comp.SourcePort))
        {
            if (TerminatingOrDeleted(uid) || !TryComp<EnergyCoreComponent>(uid, out var comp))
                continue;

            if (found)
            {
                core = default;
                return false;
            }
            found = true;
            core = (uid, comp);
        }
        return found;
    }

    private bool CanControl(Entity<EnergyCoreConsoleComponent> ent, out Entity<EnergyCoreComponent> core)
    {
        core = default;
        return Transform(ent).Anchored && TryComp<ApcPowerReceiverComponent>(ent, out var power) &&
               power.Powered && TryGetCore((ent.Owner, ent.Comp), out core);
    }

    public void SetCoreEnabled(Entity<EnergyCoreConsoleComponent?> ent, bool enabled, EntityUid? user = null)
    {
        if (!Resolve(ent, ref ent.Comp) || !CanControl((ent.Owner, ent.Comp), out var core))
            return;

        var previous = core.Comp.State;
        _cores.SetEnabled((core.Owner, core.Comp), enabled, user);
        if (core.Comp.State != previous)
            UpdateUi((ent.Owner, ent.Comp));
    }

    public void SetCoreOutput(Entity<EnergyCoreConsoleComponent?> ent, float output, EntityUid? user = null)
    {
        if (!Resolve(ent, ref ent.Comp) || !float.IsFinite(output) || !CanControl((ent.Owner, ent.Comp), out var core))
            return;

        var previous = core.Comp.TargetSupply;
        _cores.SetOutput((core.Owner, core.Comp), output, user);
        if (core.Comp.TargetSupply != previous)
            UpdateUi((ent.Owner, ent.Comp));
    }

    private void OnSetEnabledMessage(Entity<EnergyCoreConsoleComponent> ent, ref EnergyCoreSetEnabledMessage args)
    {
        if (!EnergyCoreUiKey.Key.Equals(args.UiKey) || !_ui.IsUiOpen(ent.Owner, EnergyCoreUiKey.Key, args.Actor))
            return;

        SetCoreEnabled((ent.Owner, ent.Comp), args.Enabled, args.Actor);
    }

    private void OnSetOutputMessage(Entity<EnergyCoreConsoleComponent> ent, ref EnergyCoreSetOutputMessage args)
    {
        if (!EnergyCoreUiKey.Key.Equals(args.UiKey) || !_ui.IsUiOpen(ent.Owner, EnergyCoreUiKey.Key, args.Actor))
            return;

        SetCoreOutput((ent.Owner, ent.Comp), args.Output, args.Actor);
    }

    private void OnUiOpenedEvent(Entity<EnergyCoreConsoleComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (EnergyCoreUiKey.Key.Equals(args.UiKey))
            UpdateUi(ent);
    }

    private void UpdateUi(Entity<EnergyCoreConsoleComponent> ent)
    {
        if (!_ui.IsUiOpen(ent.Owner, EnergyCoreUiKey.Key))
            return;

        var state = CanControl(ent, out var core) ? _cores.GetUiState((core.Owner, core.Comp)) : new EnergyCoreUiState(false);
        _ui.SetUiState(ent.Owner, EnergyCoreUiKey.Key, state);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        if (_timing.CurTime < _nextUpdate)
            return;
        _nextUpdate = _timing.CurTime + TimeSpan.FromSeconds(1);
        var query = EntityQueryEnumerator<EnergyCoreConsoleComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_ui.IsUiOpen(uid, EnergyCoreUiKey.Key))
                UpdateUi((uid, comp));
        }
    }
}
