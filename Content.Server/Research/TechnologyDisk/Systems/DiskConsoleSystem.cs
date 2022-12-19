using Content.Server.Research.Components;
using Content.Server.Research.Systems;
using Content.Server.Research.TechnologyDisk.Components;
using Content.Server.UserInterface;
using Content.Shared.Research;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server.Research.TechnologyDisk.Systems;

public sealed class DiskConsoleSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly ResearchSystem _research = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<DiskConsoleComponent, DiskConsolePrintDiskMessage>(OnPrintDisk);
        SubscribeLocalEvent<DiskConsoleComponent, ResearchServerPointsChangedEvent>(OnPointsChanged);
        SubscribeLocalEvent<DiskConsoleComponent, BeforeActivatableUIOpenEvent>(OnBeforeUiOpen);

        SubscribeLocalEvent<DiskConsolePrintingComponent, ComponentShutdown>(OnShutdown);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var (printing, console, xform) in EntityQuery<DiskConsolePrintingComponent, DiskConsoleComponent, TransformComponent>())
        {
            if (printing.FinishTime > _timing.CurTime)
                continue;

            RemComp(printing.Owner, printing);
            EntityManager.SpawnEntity(console.DiskPrototype, xform.Coordinates);
        }
    }

    private void OnPrintDisk(EntityUid uid, DiskConsoleComponent component, DiskConsolePrintDiskMessage args)
    {
        if (!TryComp<ResearchClientComponent>(uid, out var client) || client.Server == null)
            return;

        if (client.Server.Points < component.PricePerDisk)
            return;

        _research.ChangePointsOnServer(client.Server.Owner, -component.PricePerDisk, client.Server);
        _audio.PlayPvs(component.PrintSound, uid);

        var printing = EnsureComp<DiskConsolePrintingComponent>(uid);
        printing.FinishTime = _timing.CurTime + component.PrintDuration;
    }

    private void OnPointsChanged(EntityUid uid, DiskConsoleComponent component, ref ResearchServerPointsChangedEvent args)
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
        if (TryComp<ResearchClientComponent>(uid, out var client) && client.Server != null)
        {
            totalPoints = client.Server.Points;
        }
        var canPrint = !HasComp<DiskConsolePrintingComponent>(uid) && totalPoints >= component.PricePerDisk;

        var state = new DiskConsoleBoundUserInterfaceState(totalPoints, component.PricePerDisk, canPrint);
        _ui.TrySetUiState(uid, DiskConsoleUiKey.Key, state);
    }

    private void OnShutdown(EntityUid uid, DiskConsolePrintingComponent component, ComponentShutdown args)
    {
        UpdateUserInterface(uid);
    }
}
