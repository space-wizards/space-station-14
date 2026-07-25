// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Server.Audio;
using Content.Server.DeviceLinking.Systems;
using Content.Shared.Examine;
using Robust.Shared.Containers;
using Content.Server.DeadSpace.Virus.Components;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.DeviceLinking;
using System.Linq;
using Content.Server.Power.EntitySystems;
using Content.Shared.DeadSpace.Virus.Components;
using Robust.Server.GameObjects;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.DeadSpace.Virus;
using Robust.Shared.Prototypes;
using Content.Shared.DeadSpace.Virus.Prototypes;
using Content.Shared.Body.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Server.DeadSpace.Virus.Systems;

public sealed class VirusSolutionAnalyzerSystem : EntitySystem
{
    private static readonly TimeSpan ConsoleStatusUpdateCooldown = TimeSpan.FromSeconds(5);

    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly VirusDiagnoserConsoleSystem _console = default!;
    [Dependency] private readonly PowerReceiverSystem _powerReceiverSystem = default!;
    [Dependency] private readonly VirusDiagnoserDataServerSystem _dataServer = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly VirusEvolutionConsoleSystem _evolutionConsoleSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly DeviceLinkSystem _deviceLink = default!;
    private const string FlaskContainerKey = "flask_container_virus_solution_analyzer";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VirusSolutionAnalyzerComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<VirusSolutionAnalyzerComponent, AnchorStateChangedEvent>(OnAnchor);
        SubscribeLocalEvent<VirusSolutionAnalyzerComponent, PortDisconnectedEvent>(OnPortDisconnected);
        SubscribeLocalEvent<VirusSolutionAnalyzerComponent, EntInsertedIntoContainerMessage>(OnEntInsertCont);
        SubscribeLocalEvent<VirusSolutionAnalyzerComponent, EntRemovedFromContainerMessage>(OnEntRemoveCont);
        SubscribeLocalEvent<VirusSolutionAnalyzerComponent, ContainerIsRemovingAttemptEvent>(OnContainerRemoveAttempt);
    }

    private void OnEntInsertCont(Entity<VirusSolutionAnalyzerComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != FlaskContainerKey)
            return;

        UpdateContainerAppearance((ent, ent.Comp));
        UpdateConnectedConsole((ent, ent.Comp));
    }

    private void OnEntRemoveCont(Entity<VirusSolutionAnalyzerComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != FlaskContainerKey)
            return;

        UpdateContainerAppearance((ent, ent.Comp));
        UpdateConnectedConsole((ent, ent.Comp));
    }

    private void OnContainerRemoveAttempt(Entity<VirusSolutionAnalyzerComponent> ent, ref ContainerIsRemovingAttemptEvent args)
    {
        if (ent.Comp.Status == VirusSolutionAnalyzerStatus.Scanning &&
            args.Container.ID == FlaskContainerKey)
        {
            args.Cancel();
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<VirusSolutionAnalyzerComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!_powerReceiverSystem.IsPowered(uid))
            {
                SetStatus((uid, comp), VirusSolutionAnalyzerStatus.Off);
                continue; // без питания ничего не делаем
            }

            // Если был выключен — включаем
            if (comp.Status == VirusSolutionAnalyzerStatus.Off)
                SetStatus((uid, comp), VirusSolutionAnalyzerStatus.On);

            if (Exists(comp.CurrentSoundEntity))
            {
                UpdateConnectedConsoleThrottled((uid, comp));
                continue;
            }

            switch (comp.Status)
            {
                case VirusSolutionAnalyzerStatus.Scanning:
                    if (!CanScanning((uid, comp)))
                    {
                        SetStatus((uid, comp), VirusSolutionAnalyzerStatus.Denial);
                        break;
                    }

                    EndScanVirus((uid, comp));
                    break;

                case VirusSolutionAnalyzerStatus.Denial:
                    SetStatus((uid, comp), VirusSolutionAnalyzerStatus.On);
                    break;

                case VirusSolutionAnalyzerStatus.Successfully:
                    SetStatus((uid, comp), VirusSolutionAnalyzerStatus.On);
                    break;

                case VirusSolutionAnalyzerStatus.On:
                default:
                    break;
            }

        }
    }

    private void OnPortDisconnected(Entity<VirusSolutionAnalyzerComponent> ent, ref PortDisconnectedEvent args)
    {
        if (args.Port == ent.Comp.VirusSolutionAnalyzerPort)
        {
            var uid = ent.Owner;
            Timer.Spawn(0, () => RebuildLinkedConsoles(uid));
        }
    }

    private void RebuildLinkedConsoles(EntityUid uid)
    {
        if (!TryComp<VirusSolutionAnalyzerComponent>(uid, out var comp))
            return;

        comp.ConnectedConsole = null;
        comp.ConnectedEvolutionConsole = null;

        if (!TryComp<DeviceLinkSinkComponent>(uid, out var sink))
        {
            UpdateConnectedConsole((uid, comp));
            return;
        }

        foreach (var sourceUid in sink.LinkedSources)
        {
            var links = _deviceLink.GetLinks(sourceUid, uid);
            if (links.Count == 0)
                continue;

            if (TryComp<VirusDiagnoserConsoleComponent>(sourceUid, out var console) &&
                links.Contains((console.VirusSolutionAnalyzerPort, comp.VirusSolutionAnalyzerPort)))
            {
                comp.ConnectedConsole = sourceUid;
            }

            if (TryComp<VirusEvolutionConsoleComponent>(sourceUid, out var evolutionConsole) &&
                links.Contains((evolutionConsole.VirusSolutionAnalyzerPort, comp.VirusSolutionAnalyzerPort)))
            {
                comp.ConnectedEvolutionConsole = sourceUid;
            }
        }

        UpdateConnectedConsole((uid, comp));
    }

    private void OnAnchor(Entity<VirusSolutionAnalyzerComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (ent.Comp.ConnectedConsole != null && TryComp<VirusDiagnoserConsoleComponent>(ent.Comp.ConnectedConsole, out var console))
        {

            if (args.Anchored)
            {
                _console.RecheckConnections((ent.Comp.ConnectedConsole.Value, console));
                return;
            }

            _console.UpdateUserInterface((ent.Comp.ConnectedConsole.Value, console));
        }

        if (ent.Comp.ConnectedEvolutionConsole != null && TryComp<VirusEvolutionConsoleComponent>(ent.Comp.ConnectedEvolutionConsole, out var evolutionConsole))
        {

            if (args.Anchored)
            {
                _evolutionConsoleSystem.RecheckConnections((ent.Comp.ConnectedEvolutionConsole.Value, evolutionConsole));
                return;
            }

            _evolutionConsoleSystem.UpdateUserInterface((ent.Comp.ConnectedEvolutionConsole.Value, evolutionConsole));
        }
    }

    private void OnExamine(EntityUid uid, VirusSolutionAnalyzerComponent component, ExaminedEvent args)
    {
        BaseContainer? container = default!;

        if (_container.TryGetContainer(uid, FlaskContainerKey, out container))
        {
            if (container is ContainerSlot slot)
            {
                if (slot.ContainedEntity != null)
                    args.PushMarkup(Loc.GetString("virus-diagnoser-flask-attached"));
            }
        }
    }

    public void StartScanVirus(Entity<VirusSolutionAnalyzerComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        if (ent.Comp.Status == VirusSolutionAnalyzerStatus.Scanning)
            return;

        if (!CanScanning((ent, ent.Comp)))
        {
            SetStatus((ent, ent.Comp), VirusSolutionAnalyzerStatus.Denial);
            return;
        }

        SetStatus((ent, ent.Comp), VirusSolutionAnalyzerStatus.Scanning);
    }

    private void EndScanVirus(Entity<VirusSolutionAnalyzerComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        SetStatus((ent, ent.Comp), VirusSolutionAnalyzerStatus.Successfully);

        if (ent.Comp.ConnectedConsole == null ||
            !TryComp<VirusDiagnoserConsoleComponent>(
                ent.Comp.ConnectedConsole,
                out var console))
            return;

        if (!TryGetVirusDataFromContainer(ent, out var virusData))
            return;

        if (!TryComp<VirusDiagnoserDataServerComponent>(
                console.VirusDiagnoserDataServer,
                out var server))
            return;

        foreach (var data in virusData)
        {
            var savedStrainId = _dataServer.SaveData(
                (console.VirusDiagnoserDataServer.Value, server),
                data,
                saveModifiedAsCopy: true);

            if (!string.IsNullOrEmpty(savedStrainId))
                data.StrainId = savedStrainId;
        }

        _dataServer.UpdateConnectedInterfaces(console.VirusDiagnoserDataServer.Value, server);
    }


    public bool TryGetVirusDataFromContainer(
    EntityUid owner,
    out List<VirusData> virusData)
    {
        virusData = new();

        if (!_container.TryGetContainer(owner, FlaskContainerKey, out var container))
            return false;

        if (container is not ContainerSlot slot)
            return false;

        if (slot.ContainedEntity is not { } contained)
            return false;

        if (!TryComp<SolutionContainerManagerComponent>(contained, out var solutionManager))
            return false;

        if (!TryComp<DrawableSolutionComponent>(contained, out var drawable))
            return false;

        var wrapper = new Entity<DrawableSolutionComponent?, SolutionContainerManagerComponent?>(
            contained,
            drawable,
            solutionManager);

        if (!_solutionContainer.TryGetDrawableSolution(
                wrapper,
                out _,
                out var solution))
            return false;

        if (solution == null || solution.Contents.Count == 0)
            return false;

        foreach (var reagent in solution.Contents)
        {
            var dataList = reagent.Reagent.Data;
            if (dataList == null)
                continue;

            foreach (var data in dataList.OfType<VirusData>())
            {
                virusData.Add(data);
            }
        }

        return virusData.Count > 0;
    }

    public bool AddSymptom(Entity<VirusSolutionAnalyzerComponent?> console, string symptom)
    {
        if (!Resolve(console, ref console.Comp, false))
            return false;

        if (console.Comp.Status != VirusSolutionAnalyzerStatus.On &&
            console.Comp.Status != VirusSolutionAnalyzerStatus.Successfully)
            return false;

        if (!_prototypeManager.HasIndex<VirusSymptomPrototype>(symptom))
            return false;

        if (!TryGetVirusDataFromContainer(console, out var virusDataList))
            return false;

        var virusData = virusDataList.FirstOrDefault();

        if (virusData == null)
            return false;

        if (virusData.ActiveSymptom.Contains(symptom))
            return false;

        virusData.ActiveSymptom.Add(symptom);
        SetStatus((console, console.Comp), VirusSolutionAnalyzerStatus.Successfully);
        return true;
    }

    public bool AddBody(Entity<VirusSolutionAnalyzerComponent?> console, string body)
    {
        if (!Resolve(console, ref console.Comp, false))
            return false;

        if (console.Comp.Status != VirusSolutionAnalyzerStatus.On &&
            console.Comp.Status != VirusSolutionAnalyzerStatus.Successfully)
            return false;

        if (!_prototypeManager.HasIndex<BodyPrototype>(body))
            return false;

        if (!TryGetVirusDataFromContainer(console, out var virusDataList))
            return false;

        var virusData = virusDataList.FirstOrDefault();

        if (virusData == null)
            return false;

        if (virusData.BodyWhitelist.Contains(body))
            return false;

        virusData.BodyWhitelist.Add(body);
        SetStatus((console, console.Comp), VirusSolutionAnalyzerStatus.Successfully);
        return true;
    }

    public bool RemSymptom(Entity<VirusSolutionAnalyzerComponent?> console, string symptom)
    {
        if (!Resolve(console, ref console.Comp, false))
            return false;

        if (console.Comp.Status != VirusSolutionAnalyzerStatus.On &&
            console.Comp.Status != VirusSolutionAnalyzerStatus.Successfully)
            return false;

        if (!_prototypeManager.HasIndex<VirusSymptomPrototype>(symptom))
            return false;

        if (!TryGetVirusDataFromContainer(console, out var virusDataList))
            return false;

        var virusData = virusDataList.FirstOrDefault();

        if (virusData == null)
            return false;

        if (!virusData.ActiveSymptom.Remove(symptom))
            return false;

        SetStatus((console, console.Comp), VirusSolutionAnalyzerStatus.Successfully);
        return true;
    }

    public bool RemBody(Entity<VirusSolutionAnalyzerComponent?> console, string body)
    {
        if (!Resolve(console, ref console.Comp, false))
            return false;

        if (console.Comp.Status != VirusSolutionAnalyzerStatus.On)
            return false;

        if (!_prototypeManager.HasIndex<BodyPrototype>(body))
            return false;

        if (!TryGetVirusDataFromContainer(console, out var virusDataList))
            return false;

        var virusData = virusDataList.FirstOrDefault();

        if (virusData == null)
            return false;

        if (!virusData.BodyWhitelist.Remove(body))
            return false;

        SetStatus((console, console.Comp), VirusSolutionAnalyzerStatus.Successfully);
        return true;
    }

    private void UpdateAppearance(Entity<VirusSolutionAnalyzerComponent> ent)
    {
        if (TryComp<AppearanceComponent>(ent, out var appearance))
            _appearance.SetData(ent, VirusSolutionAnalyzerVisuals.Status, ent.Comp.Status, appearance);
    }

    private void UpdateContainerAppearance(Entity<VirusSolutionAnalyzerComponent> ent)
    {
        if (!TryComp<AppearanceComponent>(ent, out var appearance))
            return;

        if (!_container.TryGetContainer(ent, FlaskContainerKey, out var flaskContainer) ||
            flaskContainer is not ContainerSlot slot ||
            slot.ContainedEntity == null)
        {
            _appearance.SetData(ent, VirusSolutionContainerAnalyzerVisuals.Status, VirusSolutionContainerAnalyzerStatus.Empty, appearance);
            return;
        }

        _appearance.SetData(ent, VirusSolutionContainerAnalyzerVisuals.Status, VirusSolutionContainerAnalyzerStatus.Fill, appearance);
    }


    private void SetStatus(Entity<VirusSolutionAnalyzerComponent?> ent, VirusSolutionAnalyzerStatus newStatus)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        if (ent.Comp.Status == newStatus)
            return;

        if (newStatus != VirusSolutionAnalyzerStatus.On)
            QueueDel(ent.Comp.CurrentSoundEntity);

        ent.Comp.CurrentSoundEntity = null;

        switch (newStatus)
        {
            case VirusSolutionAnalyzerStatus.On:
                break;
            case VirusSolutionAnalyzerStatus.Off:
                break;
            case VirusSolutionAnalyzerStatus.Scanning:
                ent.Comp.CurrentSoundEntity = _audio.PlayPvs(ent.Comp.ScanningSound, ent)?.Entity;
                break;
            case VirusSolutionAnalyzerStatus.Denial:
                ent.Comp.CurrentSoundEntity = _audio.PlayPvs(ent.Comp.DenialSound, ent)?.Entity;
                break;
            case VirusSolutionAnalyzerStatus.Successfully:
                ent.Comp.CurrentSoundEntity = _audio.PlayPvs(ent.Comp.SuccessfullySound, ent)?.Entity;
                break;
            default:

                break;
        }

        ent.Comp.Status = newStatus;
        if (newStatus == VirusSolutionAnalyzerStatus.Scanning)
        {
            ent.Comp.ScanStartedAt = _timing.CurTime;
            ent.Comp.ScanDuration = GetScanDuration(ent.Comp.ScanningSound);
        }
        else
        {
            ent.Comp.ScanStartedAt = TimeSpan.Zero;
            ent.Comp.ScanDuration = TimeSpan.Zero;
        }

        UpdateAppearance((ent, ent.Comp));
        ent.Comp.NextConsoleStatusUpdate = newStatus == VirusSolutionAnalyzerStatus.Scanning
            ? _timing.CurTime + ConsoleStatusUpdateCooldown
            : TimeSpan.Zero;
        UpdateConnectedConsole((ent, ent.Comp));
    }

    public int GetScanProgress(Entity<VirusSolutionAnalyzerComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return 0;

        if (ent.Comp.Status != VirusSolutionAnalyzerStatus.Scanning ||
            ent.Comp.ScanDuration <= TimeSpan.Zero)
            return 0;

        var elapsed = _timing.CurTime - ent.Comp.ScanStartedAt;
        var progress = elapsed.TotalSeconds / ent.Comp.ScanDuration.TotalSeconds * 100;
        return Math.Clamp((int)Math.Round(progress), 0, 100);
    }

    private TimeSpan GetScanDuration(SoundSpecifier? sound)
    {
        if (sound == null)
            return TimeSpan.Zero;

        var duration = _audio.GetAudioLength(_audio.ResolveSound(sound));
        return duration + TimeSpan.FromSeconds(SharedAudioSystem.AudioDespawnBuffer);
    }

    private void UpdateConnectedConsoleThrottled(Entity<VirusSolutionAnalyzerComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        if (ent.Comp.Status != VirusSolutionAnalyzerStatus.Scanning)
            return;

        if (_timing.CurTime < ent.Comp.NextConsoleStatusUpdate)
            return;

        ent.Comp.NextConsoleStatusUpdate = _timing.CurTime + ConsoleStatusUpdateCooldown;
        UpdateConnectedConsole((ent, ent.Comp));
    }

    private void UpdateConnectedConsole(Entity<VirusSolutionAnalyzerComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        if (ent.Comp.ConnectedConsole != null &&
            TryComp<VirusDiagnoserConsoleComponent>(ent.Comp.ConnectedConsole, out var console))
        {
            _console.UpdateUserInterface((ent.Comp.ConnectedConsole.Value, console));
        }

        if (ent.Comp.ConnectedEvolutionConsole != null &&
            TryComp<VirusEvolutionConsoleComponent>(ent.Comp.ConnectedEvolutionConsole, out var evolutionConsole))
        {
            _evolutionConsoleSystem.UpdateUserInterface((ent.Comp.ConnectedEvolutionConsole.Value, evolutionConsole));
        }
    }

    public bool CanScanning(Entity<VirusSolutionAnalyzerComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        if (!_container.TryGetContainer(ent, FlaskContainerKey, out var flaskContainer))
            return false;

        if (flaskContainer is not ContainerSlot slot)
            return false;

        if (slot.ContainedEntity == null)
            return false;

        return true;
    }

}
