using Content.Shared.UserInterface;
using Content.Shared.Research.Components;
using Content.Shared.Research.Systems;
using Content.Shared.Research.TechnologyDisk.Components;
using Robust.Shared.Timing;
using Robust.Shared.Audio.Systems;

namespace Content.Shared.Research.TechnologyDisk.Systems;

public sealed class DiskConsoleSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ResearchSystem _research = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<DiskConsoleComponent, DiskConsolePrintDiskMessage>(OnPrintDisk);
        SubscribeLocalEvent<DiskConsoleComponent, ResearchServerPointsChangedEvent>(OnPointsChanged);
        SubscribeLocalEvent<DiskConsoleComponent, ResearchRegistrationChangedEvent>(OnRegistrationChanged);
        SubscribeLocalEvent<DiskConsoleComponent, BeforeActivatableUIOpenEvent>(OnBeforeUiOpen);

        SubscribeLocalEvent<DiskConsolePrintingComponent, ComponentShutdown>(OnShutdown);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<DiskConsolePrintingComponent, DiskConsoleComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var printing, out var console, out var xform))
        {
            if (printing.FinishTime > _timing.CurTime)
                continue;

            RemComp(uid, printing);
            PredictedSpawnAttachedTo(console.DiskPrototype, xform.Coordinates);
        }
    }

    private void OnPrintDisk(Entity<DiskConsoleComponent> ent, ref DiskConsolePrintDiskMessage args)
    {
        if (HasComp<DiskConsolePrintingComponent>(ent))
            return;

        if (!TryComp<ResearchClientComponent>(ent, out var client))
            return;

        if (!_research.TryGetClientServer((ent, client), out var server))
            return;

        if (server.Value.Comp.Points < ent.Comp.PricePerDisk)
            return;

        _research.ModifyServerPoints(server.Value.AsNullable(), -ent.Comp.PricePerDisk);
        _audio.PlayPredicted(ent.Comp.PrintSound, ent, ent);

        var printing = EnsureComp<DiskConsolePrintingComponent>(ent);
        printing.FinishTime = _timing.CurTime + ent.Comp.PrintDuration;
        Dirty(ent, printing);
        UpdateUserInterface(ent.AsNullable());
    }

    private void OnPointsChanged(Entity<DiskConsoleComponent> ent, ref ResearchServerPointsChangedEvent args)
    {
        UpdateUserInterface(ent.AsNullable());
    }

    private void OnRegistrationChanged(Entity<DiskConsoleComponent> ent, ref ResearchRegistrationChangedEvent args)
    {
        UpdateUserInterface(ent.AsNullable());
    }

    private void OnBeforeUiOpen(Entity<DiskConsoleComponent> ent, ref BeforeActivatableUIOpenEvent args)
    {
        UpdateUserInterface(ent.AsNullable());
    }

    public void UpdateUserInterface(Entity<DiskConsoleComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        if (!TryComp<ResearchClientComponent>(ent, out var client))
            return;

        var totalPoints = 0;
        if (_research.TryGetClientServer((ent, client), out var server))
        {
            totalPoints = server.Value.Comp.Points;
        }

        var canPrint = !(TryComp<DiskConsolePrintingComponent>(ent, out var printing) && printing.FinishTime >= _timing.CurTime) &&
                       totalPoints >= ent.Comp.PricePerDisk;

        var state = new DiskConsoleBoundUserInterfaceState(totalPoints, ent.Comp.PricePerDisk, canPrint);
        _ui.SetUiState(ent.Owner, DiskConsoleUiKey.Key, state);
    }

    private void OnShutdown(Entity<DiskConsolePrintingComponent> ent, ref ComponentShutdown args)
    {
        UpdateUserInterface(ent.Owner);
    }
}
