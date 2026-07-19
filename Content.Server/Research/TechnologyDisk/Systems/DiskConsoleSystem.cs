using Content.Server.Research.Systems;
using Content.Server.Research.TechnologyDisk.Components;
using Content.Shared.UserInterface;
using Content.Shared.Research;
using Content.Shared.Research.Components;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server.Research.TechnologyDisk.Systems;

public sealed partial class DiskConsoleSystem : EntitySystem
{
    private static readonly EntityTimerId PrintTimer = new("print");

    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private ResearchSystem _research = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;
    [Dependency] private IEntityTimerManager _timers = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<DiskConsoleComponent, DiskConsolePrintDiskMessage>(OnPrintDisk);
        SubscribeLocalEvent<DiskConsoleComponent, ResearchServerPointsChangedEvent>(OnPointsChanged);
        SubscribeLocalEvent<DiskConsoleComponent, ResearchRegistrationChangedEvent>(OnRegistrationChanged);
        SubscribeLocalEvent<DiskConsoleComponent, BeforeActivatableUIOpenEvent>(OnBeforeUiOpen);

            SubscribeLocalEvent<DiskConsolePrintingComponent, ComponentShutdown>(OnShutdown);
            SubscribeLocalEvent<DiskConsolePrintingComponent, ComponentStartup>(OnPrintingStartup);
            SubscribeLocalEvent<DiskConsolePrintingComponent, EntityTimerEvent>(OnTimer);
    }

    private void OnPrintingStartup(Entity<DiskConsolePrintingComponent> ent, ref ComponentStartup args)
    {
        _timers.SetTimerAt(ent, PrintTimer, ent.Comp.FinishTime);
    }

    private void OnTimer(Entity<DiskConsolePrintingComponent> ent, ref EntityTimerEvent args)
    {
        if (args.Id != PrintTimer || !TryComp<DiskConsoleComponent>(ent, out var console) ||
            !TryComp<TransformComponent>(ent, out var xform))
            return;

        RemComp(ent, ent.Comp);
        Spawn(console.DiskPrototype, xform.Coordinates);
    }

    private void OnPrintDisk(EntityUid uid, DiskConsoleComponent component, DiskConsolePrintDiskMessage args)
    {
        if (HasComp<DiskConsolePrintingComponent>(uid))
            return;

        if (!_research.TryGetClientServer(uid, out var server, out var serverComp))
            return;

        if (serverComp.Points < component.PricePerDisk)
            return;

        _research.ModifyServerPoints(server.Value, -component.PricePerDisk, serverComp);
        _audio.PlayPvs(component.PrintSound, uid);

        var printing = EnsureComp<DiskConsolePrintingComponent>(uid);
        printing.FinishTime = _timing.CurTime + component.PrintDuration;
        _timers.SetTimerAt<DiskConsolePrintingComponent>((uid, printing), PrintTimer, printing.FinishTime);
        UpdateUserInterface(uid, component);
    }

    private void OnPointsChanged(EntityUid uid, DiskConsoleComponent component, ref ResearchServerPointsChangedEvent args)
    {
        UpdateUserInterface(uid, component);
    }

    private void OnRegistrationChanged(EntityUid uid, DiskConsoleComponent component, ref ResearchRegistrationChangedEvent args)
    {
        UpdateUserInterface(uid, component);
    }

    private void OnBeforeUiOpen(EntityUid uid, DiskConsoleComponent component, BeforeActivatableUIOpenEvent args)
    {
        UpdateUserInterface(uid, component);
    }

    public void UpdateUserInterface(EntityUid uid, DiskConsoleComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        var totalPoints = 0;
        if (_research.TryGetClientServer(uid, out _, out var server))
        {
            totalPoints = server.Points;
        }

        var canPrint = !HasComp<DiskConsolePrintingComponent>(uid) &&
                       totalPoints >= component.PricePerDisk;

        var state = new DiskConsoleBoundUserInterfaceState(totalPoints, component.PricePerDisk, canPrint);
        _ui.SetUiState(uid, DiskConsoleUiKey.Key, state);
    }

    private void OnShutdown(EntityUid uid, DiskConsolePrintingComponent component, ComponentShutdown args)
    {
        UpdateUserInterface(uid);
    }
}
