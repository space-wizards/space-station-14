using System.Linq;
using Content.Server.Construction;
using Content.Server.Paper;
using Content.Server.Power.Components;
using Content.Server.Research.Systems;
using Content.Server.UserInterface;
using Content.Server.Xenoarchaeology.Equipment.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Content.Shared.Audio;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Placeable;
using Content.Shared.Popups;
using Content.Shared.Research.Components;
using Content.Shared.Xenoarchaeology.Equipment;
using Content.Shared.Xenoarchaeology.XenoArtifacts;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Xenoarchaeology.Equipment.Systems;

/// <summary>
/// This system is used for managing the artifact analyzer as well as the analysis console.
/// It also hanadles scanning and ui updates for both systems.
/// </summary>
public sealed class ArtifactAnalyzerSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedAmbientSoundSystem _ambientSound = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly ArtifactSystem _artifact = default!;
    [Dependency] private readonly PaperSystem _paper = default!;
    [Dependency] private readonly ResearchSystem _research = default!;
    [Dependency] private readonly MetaDataSystem _metaSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ActiveScannedArtifactComponent, ArtifactActivatedEvent>(OnArtifactActivated);

        SubscribeLocalEvent<ActiveArtifactAnalyzerComponent, ComponentStartup>(OnAnalyzeStart);
        SubscribeLocalEvent<ActiveArtifactAnalyzerComponent, ComponentShutdown>(OnAnalyzeEnd);
        SubscribeLocalEvent<ActiveArtifactAnalyzerComponent, PowerChangedEvent>(OnPowerChanged);

        SubscribeLocalEvent<ArtifactAnalyzerComponent, ItemPlacedEvent>(OnItemPlaced);
        SubscribeLocalEvent<ArtifactAnalyzerComponent, ItemRemovedEvent>(OnItemRemoved);

        SubscribeLocalEvent<ArtifactAnalyzerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<AnalysisConsoleComponent, NewLinkEvent>(OnNewLink);
        SubscribeLocalEvent<AnalysisConsoleComponent, PortDisconnectedEvent>(OnPortDisconnected);

        SubscribeLocalEvent<AnalysisConsoleComponent, AnalysisConsoleServerSelectionMessage>(OnServerSelectionMessage);
        SubscribeLocalEvent<AnalysisConsoleComponent, AnalysisConsoleScanButtonPressedMessage>(OnScanButton);
        SubscribeLocalEvent<AnalysisConsoleComponent, AnalysisConsolePrintButtonPressedMessage>(OnPrintButton);
        SubscribeLocalEvent<AnalysisConsoleComponent, AnalysisConsoleExtractButtonPressedMessage>(OnExtractButton);

        SubscribeLocalEvent<AnalysisConsoleComponent, ResearchClientServerSelectedMessage>((e, c, _) => UpdateUserInterface(e, c),
            after: new[] { typeof(ResearchSystem) });
        SubscribeLocalEvent<AnalysisConsoleComponent, ResearchClientServerDeselectedMessage>((e, c, _) => UpdateUserInterface(e, c),
            after: new[] { typeof(ResearchSystem) });
        SubscribeLocalEvent<AnalysisConsoleComponent, BeforeActivatableUIOpenEvent>((e, c, _) => UpdateUserInterface(e, c));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ActiveArtifactAnalyzerComponent, ArtifactAnalyzerComponent>();
        while (query.MoveNext(out var uid, out var active, out var scan))
        {
            if (active.AnalysisPaused)
                continue;

            if (_timing.CurTime - active.StartTime < scan.AnalysisDuration - active.AccumulatedRunTime)
                continue;

            FinishScan(uid, scan, active);
        }
    }

    /// <summary>
    /// Resets the current scan on the artifact analyzer
    /// </summary>
    /// <param name="uid">The analyzer being reset</param>
    /// <param name="component"></param>
    [PublicAPI]
    public void ResetAnalyzer(EntityUid uid, ArtifactAnalyzerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.LastAnalyzedArtifact = null;
        component.ReadyToPrint = false;
        UpdateAnalyzerInformation(uid, component);
    }

    /// <summary>
    /// Goes through the current entities on
    /// the analyzer and returns a valid artifact
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="placer"></param>
    /// <returns></returns>
    private EntityUid? GetArtifactForAnalysis(EntityUid? uid, ItemPlacerComponent? placer = null)
    {
        if (uid == null || !Resolve(uid.Value, ref placer))
            return null;

        return placer.PlacedEntities.FirstOrNull();
    }

    /// <summary>
    /// Updates the current scan information based on
    /// the last artifact that was scanned.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="component"></param>
    private void UpdateAnalyzerInformation(EntityUid uid, ArtifactAnalyzerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.LastAnalyzedArtifact == null)
        {
            component.LastAnalyzerPointValue = null;
            component.LastAnalyzedNode = null;
        }
        else if (TryComp<ArtifactComponent>(component.LastAnalyzedArtifact, out var artifact))
        {
            var lastNode = artifact.CurrentNodeId == null
                ? null
                : (ArtifactNode?) _artifact.GetNodeFromId(artifact.CurrentNodeId.Value, artifact).Clone();
            component.LastAnalyzedNode = lastNode;
            component.LastAnalyzerPointValue = _artifact.GetResearchPointValue(component.LastAnalyzedArtifact.Value, artifact);
        }
    }

    private void OnMapInit(EntityUid uid, ArtifactAnalyzerComponent component, MapInitEvent args)
    {
        if (!TryComp<DeviceLinkSinkComponent>(uid, out var sink))
            return;

        foreach (var source in sink.LinkedSources)
        {
            if (!TryComp<AnalysisConsoleComponent>(source, out var analysis))
                continue;
            component.Console = source;
            analysis.AnalyzerEntity = uid;
            return;
        }
    }

    private void OnNewLink(EntityUid uid, AnalysisConsoleComponent component, NewLinkEvent args)
    {
        if (!TryComp<ArtifactAnalyzerComponent>(args.Sink, out var analyzer))
            return;

        component.AnalyzerEntity = args.Sink;
        analyzer.Console = uid;

        UpdateUserInterface(uid, component);
    }

    private void OnPortDisconnected(EntityUid uid, AnalysisConsoleComponent component, PortDisconnectedEvent args)
    {
        if (args.Port == component.LinkingPort && component.AnalyzerEntity != null)
        {
            if (TryComp<ArtifactAnalyzerComponent>(component.AnalyzerEntity, out var analyzezr))
                analyzezr.Console = null;
            component.AnalyzerEntity = null;
        }

        UpdateUserInterface(uid, component);
    }

    private void UpdateUserInterface(EntityUid uid, AnalysisConsoleComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        EntityUid? artifact = null;
        FormattedMessage? msg = null;
        TimeSpan? totalTime = null;
        var canScan = false;
        var canPrint = false;
        var points = 0;
        if (TryComp<ArtifactAnalyzerComponent>(component.AnalyzerEntity, out var analyzer))
        {
            artifact = analyzer.LastAnalyzedArtifact;
            msg = GetArtifactScanMessage(analyzer);
            totalTime = analyzer.AnalysisDuration;
            if (TryComp<ItemPlacerComponent>(component.AnalyzerEntity, out var placer))
                canScan = placer.PlacedEntities.Any();
            canPrint = analyzer.ReadyToPrint;

            // the artifact that's actually on the scanner right now.
            if (GetArtifactForAnalysis(component.AnalyzerEntity, placer) is { } current)
                points = _artifact.GetResearchPointValue(current);
        }
        var analyzerConnected = component.AnalyzerEntity != null;
        var serverConnected = TryComp<ResearchClientComponent>(uid, out var client) && client.ConnectedToServer;

        var scanning = TryComp<ActiveArtifactAnalyzerComponent>(component.AnalyzerEntity, out var active);
        var paused = active != null ? active.AnalysisPaused : false;


        var state = new AnalysisConsoleScanUpdateState(GetNetEntity(artifact), analyzerConnected, serverConnected,
            canScan, canPrint, msg, scanning, paused, active?.StartTime, active?.AccumulatedRunTime, totalTime, points);

        var bui = _ui.GetUi(uid, ArtifactAnalzyerUiKey.Key);
        _ui.SetUiState(bui, state);
    }

    /// <summary>
    /// opens the server selection menu.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="component"></param>
    /// <param name="args"></param>
    private void OnServerSelectionMessage(EntityUid uid, AnalysisConsoleComponent component, AnalysisConsoleServerSelectionMessage args)
    {
        _ui.TryOpen(uid, ResearchClientUiKey.Key, args.Session);
    }

    /// <summary>
    /// Starts scanning the artifact.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="component"></param>
    /// <param name="args"></param>
    private void OnScanButton(EntityUid uid, AnalysisConsoleComponent component, AnalysisConsoleScanButtonPressedMessage args)
    {
        if (component.AnalyzerEntity == null)
            return;

        if (HasComp<ActiveArtifactAnalyzerComponent>(component.AnalyzerEntity))
            return;

        var ent = GetArtifactForAnalysis(component.AnalyzerEntity);
        if (ent == null)
            return;

        var activeComp = EnsureComp<ActiveArtifactAnalyzerComponent>(component.AnalyzerEntity.Value);
        activeComp.StartTime = _timing.CurTime;
        activeComp.AccumulatedRunTime = TimeSpan.Zero;
        activeComp.Artifact = ent.Value;

        if (TryComp<ApcPowerReceiverComponent>(component.AnalyzerEntity.Value, out var powa))
            activeComp.AnalysisPaused = !powa.Powered;

        var activeArtifact = EnsureComp<ActiveScannedArtifactComponent>(ent.Value);
        activeArtifact.Scanner = component.AnalyzerEntity.Value;
        UpdateUserInterface(uid, component);
    }

    private void OnPrintButton(EntityUid uid, AnalysisConsoleComponent component, AnalysisConsolePrintButtonPressedMessage args)
    {
        if (component.AnalyzerEntity == null)
            return;

        if (!TryComp<ArtifactAnalyzerComponent>(component.AnalyzerEntity, out var analyzer) ||
            analyzer.LastAnalyzedNode == null ||
            analyzer.LastAnalyzerPointValue == null ||
            !analyzer.ReadyToPrint)
        {
            return;
        }
        analyzer.ReadyToPrint = false;

        var report = Spawn(component.ReportEntityId, Transform(uid).Coordinates);
        _metaSystem.SetEntityName(report, Loc.GetString("analysis-report-title", ("id", analyzer.LastAnalyzedNode.Id)));

        var msg = GetArtifactScanMessage(analyzer);
        if (msg == null)
            return;

        _popup.PopupEntity(Loc.GetString("analysis-console-print-popup"), uid);
        _paper.SetContent(report, msg.ToMarkup());
        UpdateUserInterface(uid, component);
    }

    private FormattedMessage? GetArtifactScanMessage(ArtifactAnalyzerComponent component)
    {
        var msg = new FormattedMessage();
        if (component.LastAnalyzedNode == null)
            return null;

        var n = component.LastAnalyzedNode;

        msg.AddMarkup(Loc.GetString("analysis-console-info-id", ("id", n.Id)));
        msg.PushNewline();
        msg.AddMarkup(Loc.GetString("analysis-console-info-depth", ("depth", n.Depth)));
        msg.PushNewline();

        var activated = n.Triggered
            ? "analysis-console-info-triggered-true"
            : "analysis-console-info-triggered-false";
        msg.AddMarkup(Loc.GetString(activated));
        msg.PushNewline();

        msg.PushNewline();
        var needSecondNewline = false;

        var triggerProto = _prototype.Index<ArtifactTriggerPrototype>(n.Trigger);
        if (triggerProto.TriggerHint != null)
        {
            msg.AddMarkup(Loc.GetString("analysis-console-info-trigger",
                ("trigger", Loc.GetString(triggerProto.TriggerHint))) + "\n");
            needSecondNewline = true;
        }

        var effectproto = _prototype.Index<ArtifactEffectPrototype>(n.Effect);
        if (effectproto.EffectHint != null)
        {
            msg.AddMarkup(Loc.GetString("analysis-console-info-effect",
                ("effect", Loc.GetString(effectproto.EffectHint))) + "\n");
            needSecondNewline = true;
        }

        if (needSecondNewline)
            msg.PushNewline();

        msg.AddMarkup(Loc.GetString("analysis-console-info-edges", ("edges", n.Edges.Count)));
        msg.PushNewline();

        if (component.LastAnalyzerPointValue != null)
            msg.AddMarkup(Loc.GetString("analysis-console-info-value", ("value", component.LastAnalyzerPointValue)));

        return msg;
    }

    /// <summary>
    /// Extracts points from the artifact and updates the server points
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="component"></param>
    /// <param name="args"></param>
    private void OnExtractButton(EntityUid uid, AnalysisConsoleComponent component, AnalysisConsoleExtractButtonPressedMessage args)
    {
        if (component.AnalyzerEntity == null)
            return;

        if (!_research.TryGetClientServer(uid, out var server, out var serverComponent))
            return;

        var artifact = GetArtifactForAnalysis(component.AnalyzerEntity);
        if (artifact == null)
            return;

        var pointValue = _artifact.GetResearchPointValue(artifact.Value);

        // no new nodes triggered so nothing to add
        if (pointValue == 0)
            return;

        _research.ModifyServerPoints(server.Value, pointValue, serverComponent);
        _artifact.AdjustConsumedPoints(artifact.Value, pointValue);

        _audio.PlayPvs(component.ExtractSound, component.AnalyzerEntity.Value, AudioParams.Default.WithVolume(2f));

        _popup.PopupEntity(Loc.GetString("analyzer-artifact-extract-popup"),
            component.AnalyzerEntity.Value, PopupType.Large);

        UpdateUserInterface(uid, component);
    }

    /// <summary>
    /// Cancels scans if the artifact changes nodes (is activated) during the scan.
    /// </summary>
    private void OnArtifactActivated(EntityUid uid, ActiveScannedArtifactComponent component, ArtifactActivatedEvent args)
    {
        CancelScan(uid);
    }

    /// <summary>
    /// Stops the current scan
    /// </summary>
    [PublicAPI]
    public void CancelScan(EntityUid artifact, ActiveScannedArtifactComponent? component = null, ArtifactAnalyzerComponent? analyzer = null)
    {
        if (!Resolve(artifact, ref component, false))
            return;

        if (!Resolve(component.Scanner, ref analyzer))
            return;

        _audio.PlayPvs(component.ScanFailureSound, component.Scanner, AudioParams.Default.WithVolume(3f));

        RemComp<ActiveArtifactAnalyzerComponent>(component.Scanner);
        if (analyzer.Console != null)
            UpdateUserInterface(analyzer.Console.Value);

        RemCompDeferred(artifact, component);
    }

    /// <summary>
    /// Finishes the current scan.
    /// </summary>
    [PublicAPI]
    public void FinishScan(EntityUid uid, ArtifactAnalyzerComponent? component = null, ActiveArtifactAnalyzerComponent? active = null)
    {
        if (!Resolve(uid, ref component, ref active))
            return;

        component.ReadyToPrint = true;
        _audio.PlayPvs(component.ScanFinishedSound, uid);
        component.LastAnalyzedArtifact = active.Artifact;
        UpdateAnalyzerInformation(uid, component);

        RemComp<ActiveScannedArtifactComponent>(active.Artifact);
        RemComp(uid, active);
        if (component.Console != null)
            UpdateUserInterface(component.Console.Value);
    }

    [PublicAPI]
    public void PauseScan(EntityUid uid, ArtifactAnalyzerComponent? component = null, ActiveArtifactAnalyzerComponent? active = null)
    {
        if (!Resolve(uid, ref component, ref active) || active.AnalysisPaused)
            return;

        active.AnalysisPaused = true;
        // As we pause, we store what was already completed.
        active.AccumulatedRunTime = (_timing.CurTime - active.StartTime) + active.AccumulatedRunTime;

        if (Exists(component.Console))
            UpdateUserInterface(component.Console.Value);
    }

    [PublicAPI]
    public void ResumeScan(EntityUid uid, ArtifactAnalyzerComponent? component = null, ActiveArtifactAnalyzerComponent? active = null)
    {
        if (!Resolve(uid, ref component, ref active) || !active.AnalysisPaused)
            return;

        active.StartTime = _timing.CurTime;
        active.AnalysisPaused = false;

        if (Exists(component.Console))
            UpdateUserInterface(component.Console.Value);
    }

    private void OnItemPlaced(EntityUid uid, ArtifactAnalyzerComponent component, ref ItemPlacedEvent args)
    {
        if (component.Console != null && Exists(component.Console))
            UpdateUserInterface(component.Console.Value);
    }

    private void OnItemRemoved(EntityUid uid, ArtifactAnalyzerComponent component, ref ItemRemovedEvent args)
    {
        // Scanners shouldn't give permanent remove vision to an artifact, and the scanned artifact doesn't have any
        // component to track analyzers that have scanned it for removal if the artifact gets deleted.
        // So we always clear this on removal.
        component.LastAnalyzedArtifact = null;

        // cancel the scan if the artifact moves off the analyzer
        CancelScan(args.OtherEntity);
        if (Exists(component.Console))
            UpdateUserInterface(component.Console.Value);
    }

    private void OnAnalyzeStart(EntityUid uid, ActiveArtifactAnalyzerComponent component, ComponentStartup args)
    {
        if (TryComp<ApcPowerReceiverComponent>(uid, out var powa))
            powa.NeedsPower = true;

        _ambientSound.SetAmbience(uid, true);
    }

    private void OnAnalyzeEnd(EntityUid uid, ActiveArtifactAnalyzerComponent component, ComponentShutdown args)
    {
        if (TryComp<ApcPowerReceiverComponent>(uid, out var powa))
            powa.NeedsPower = false;

        _ambientSound.SetAmbience(uid, false);
    }

    private void OnPowerChanged(EntityUid uid, ActiveArtifactAnalyzerComponent active, ref PowerChangedEvent args)
    {
        if (!args.Powered)
        {
            PauseScan(uid, null, active);
        }
        else
        {
            ResumeScan(uid, null, active);
        }
    }
}

