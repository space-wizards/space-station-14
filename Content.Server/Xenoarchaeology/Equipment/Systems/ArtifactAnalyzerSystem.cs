using System.Linq;
using Content.Server.Construction;
using Content.Server.MachineLinking.Components;
using Content.Server.MachineLinking.Events;
using Content.Server.Paper;
using Content.Server.Power.Components;
using Content.Server.Research;
using Content.Server.Research.Components;
using Content.Server.UserInterface;
using Content.Server.Xenoarchaeology.Equipment.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Content.Shared.Audio;
using Content.Shared.MachineLinking.Events;
using Content.Shared.Popups;
using Content.Shared.Research.Components;
using Content.Shared.Xenoarchaeology.Equipment;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;
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
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly ArtifactSystem _artifact = default!;
    [Dependency] private readonly PaperSystem _paper = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ActiveScannedArtifactComponent, MoveEvent>(OnScannedMoved);
        SubscribeLocalEvent<ActiveScannedArtifactComponent, ArtifactActivatedEvent>(OnArtifactActivated);

        SubscribeLocalEvent<ActiveArtifactAnalyzerComponent, ComponentStartup>(OnAnalyzeStart);
        SubscribeLocalEvent<ActiveArtifactAnalyzerComponent, ComponentShutdown>(OnAnalyzeEnd);
        SubscribeLocalEvent<ActiveArtifactAnalyzerComponent, PowerChangedEvent>(OnPowerChanged);

        SubscribeLocalEvent<ArtifactAnalyzerComponent, UpgradeExamineEvent>(OnUpgradeExamine);
        SubscribeLocalEvent<ArtifactAnalyzerComponent, RefreshPartsEvent>(OnRefreshParts);
        SubscribeLocalEvent<ArtifactAnalyzerComponent, StartCollideEvent>(OnCollide);
        SubscribeLocalEvent<ArtifactAnalyzerComponent, EndCollideEvent>(OnEndCollide);

        SubscribeLocalEvent<ArtifactAnalyzerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<AnalysisConsoleComponent, NewLinkEvent>(OnNewLink);
        SubscribeLocalEvent<AnalysisConsoleComponent, PortDisconnectedEvent>(OnPortDisconnected);

        SubscribeLocalEvent<AnalysisConsoleComponent, AnalysisConsoleServerSelectionMessage>(OnServerSelectionMessage);
        SubscribeLocalEvent<AnalysisConsoleComponent, AnalysisConsoleScanButtonPressedMessage>(OnScanButton);
        SubscribeLocalEvent<AnalysisConsoleComponent, AnalysisConsolePrintButtonPressedMessage>(OnPrintButton);
        SubscribeLocalEvent<AnalysisConsoleComponent, AnalysisConsoleDestroyButtonPressedMessage>(OnDestroyButton);

        SubscribeLocalEvent<AnalysisConsoleComponent, ResearchClientServerSelectedMessage>((e,c,_) => UpdateUserInterface(e,c),
            after: new []{typeof(ResearchSystem)});
        SubscribeLocalEvent<AnalysisConsoleComponent, ResearchClientServerDeselectedMessage>((e,c,_) => UpdateUserInterface(e,c),
            after: new []{typeof(ResearchSystem)});
        SubscribeLocalEvent<AnalysisConsoleComponent, BeforeActivatableUIOpenEvent>((e,c,_) => UpdateUserInterface(e,c));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var (active, scan) in EntityQuery<ActiveArtifactAnalyzerComponent, ArtifactAnalyzerComponent>())
        {
            if (scan.Console != null)
                UpdateUserInterface(scan.Console.Value);

            if (_timing.CurTime - active.StartTime < (scan.AnalysisDuration * scan.AnalysisDurationMulitplier))
                continue;

            FinishScan(scan.Owner, scan, active);
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
    /// Goes through the current contacts on
    /// the analyzer and returns a valid artifact
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="component"></param>
    /// <returns></returns>
    private EntityUid? GetArtifactForAnalysis(EntityUid? uid, ArtifactAnalyzerComponent? component = null)
    {
        if (uid == null)
            return null;

        if (!Resolve(uid.Value, ref component))
            return null;

        var validEnts = component.Contacts.Where(HasComp<ArtifactComponent>).ToHashSet();
        return validEnts.FirstOrNull();
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
            var lastNode = (ArtifactNode?) artifact.CurrentNode?.Clone();
            component.LastAnalyzedNode = lastNode;
            component.LastAnalyzerPointValue = _artifact.GetResearchPointValue(component.LastAnalyzedArtifact.Value, artifact);
        }
    }

    private void OnMapInit(EntityUid uid, ArtifactAnalyzerComponent component, MapInitEvent args)
    {
        if (!TryComp<SignalReceiverComponent>(uid, out var receiver))
            return;

        foreach (var port in receiver.Inputs.Values.SelectMany(ports => ports))
        {
            if (!TryComp<AnalysisConsoleComponent>(port.Uid, out var analysis))
                continue;
            component.Console = port.Uid;
            analysis.AnalyzerEntity = uid;
            return;
        }
    }

    private void OnNewLink(EntityUid uid, AnalysisConsoleComponent component, NewLinkEvent args)
    {
        if (!TryComp<ArtifactAnalyzerComponent>(args.Receiver, out var analyzer))
            return;

        component.AnalyzerEntity = args.Receiver;
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
        var totalTime = TimeSpan.Zero;
        var canScan = false;
        var canPrint = false;
        if (component.AnalyzerEntity != null && TryComp<ArtifactAnalyzerComponent>(component.AnalyzerEntity, out var analyzer))
        {
            artifact = analyzer.LastAnalyzedArtifact;
            msg = GetArtifactScanMessage(analyzer);
            totalTime = analyzer.AnalysisDuration * analyzer.AnalysisDurationMulitplier;
            canScan = analyzer.Contacts.Any();
            canPrint = analyzer.ReadyToPrint;
        }
        var analyzerConnected = component.AnalyzerEntity != null;
        var serverConnected = TryComp<ResearchClientComponent>(uid, out var client) && client.ConnectedToServer;

        var scanning = TryComp<ActiveArtifactAnalyzerComponent>(component.AnalyzerEntity, out var active);
        var remaining = active != null ? _timing.CurTime - active.StartTime : TimeSpan.Zero;

        var state = new AnalysisConsoleScanUpdateState(artifact, analyzerConnected, serverConnected,
            canScan, canPrint, msg, scanning, remaining, totalTime);

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
        _ui.TryOpen(uid, ResearchClientUiKey.Key, (IPlayerSession) args.Session);
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
        activeComp.Artifact = ent.Value;

        var activeArtifact = EnsureComp<ActiveScannedArtifactComponent>(ent.Value);
        activeArtifact.Scanner = component.AnalyzerEntity.Value;
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
        MetaData(report).EntityName = Loc.GetString("analysis-report-title", ("id", analyzer.LastAnalyzedNode.Id));

        var msg = GetArtifactScanMessage(analyzer);
        if (msg == null)
            return;

        _popup.PopupEntity(Loc.GetString("analysis-console-print-popup"), uid, Filter.Pvs(uid));
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
        if (n.Trigger.TriggerHint != null)
        {
            msg.AddMarkup(Loc.GetString("analysis-console-info-trigger",
                ("trigger", Loc.GetString(n.Trigger.TriggerHint))) + "\n");
            needSecondNewline = true;
        }

        if (n.Effect.EffectHint != null)
        {
            msg.AddMarkup(Loc.GetString("analysis-console-info-effect",
                ("effect", Loc.GetString(n.Effect.EffectHint))) + "\n");
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
    /// destroys the artifact and updates the server points
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="component"></param>
    /// <param name="args"></param>
    private void OnDestroyButton(EntityUid uid, AnalysisConsoleComponent component, AnalysisConsoleDestroyButtonPressedMessage args)
    {
        if (!TryComp<ResearchClientComponent>(uid, out var client) || client.Server == null || component.AnalyzerEntity == null)
            return;

        var entToDestroy = GetArtifactForAnalysis(component.AnalyzerEntity);
        if (entToDestroy == null)
            return;

        if (TryComp<ArtifactAnalyzerComponent>(component.AnalyzerEntity.Value, out var analyzer) &&
            analyzer.LastAnalyzedArtifact == entToDestroy)
        {
            ResetAnalyzer(component.AnalyzerEntity.Value);
        }

        client.Server.Points += _artifact.GetResearchPointValue(entToDestroy.Value);
        EntityManager.DeleteEntity(entToDestroy.Value);

        _audio.PlayPvs(component.DestroySound, component.AnalyzerEntity.Value, AudioParams.Default.WithVolume(2f));

        _popup.PopupEntity(Loc.GetString("analyzer-artifact-destroy-popup"),
            component.AnalyzerEntity.Value, Filter.Pvs(component.AnalyzerEntity.Value), PopupType.Large);

        UpdateUserInterface(uid, component);
    }

    /// <summary>
    /// Cancels scans if the artifact changes nodes (is activated) during the scan.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="component"></param>
    /// <param name="args"></param>
    private void OnArtifactActivated(EntityUid uid, ActiveScannedArtifactComponent component, ArtifactActivatedEvent args)
    {
        CancelScan(uid);
    }

    /// <summary>
    /// Checks to make sure that the currently scanned artifact isn't moved off of the scanner
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="component"></param>
    /// <param name="args"></param>
    private void OnScannedMoved(EntityUid uid, ActiveScannedArtifactComponent component, ref MoveEvent args)
    {
        if (!TryComp<ArtifactAnalyzerComponent>(component.Scanner, out var analyzer))
            return;

        if (analyzer.Contacts.Contains(uid))
            return;

        CancelScan(uid, component, analyzer);
    }

    /// <summary>
    /// Stops the current scan
    /// </summary>
    /// <param name="artifact">The artifact being scanned</param>
    /// <param name="component"></param>
    /// <param name="analyzer">The artifact analyzer component</param>
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
    /// <param name="uid">The analyzer that is scanning</param>
    /// <param name="component"></param>
    /// <param name="active"></param>
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

    private void OnRefreshParts(EntityUid uid, ArtifactAnalyzerComponent component, RefreshPartsEvent args)
    {
        var analysisRating = args.PartRatings[component.MachinePartAnalysisDuration];

        component.AnalysisDurationMulitplier = MathF.Pow(component.PartRatingAnalysisDurationMultiplier, analysisRating - 1);
    }

    private void OnUpgradeExamine(EntityUid uid, ArtifactAnalyzerComponent component, UpgradeExamineEvent args)
    {
        args.AddPercentageUpgrade("analyzer-artifact-component-upgrade-analysis", component.AnalysisDurationMulitplier);
    }

    private void OnCollide(EntityUid uid, ArtifactAnalyzerComponent component, ref StartCollideEvent args)
    {
        var otherEnt = args.OtherFixture.Body.Owner;

        if (!HasComp<ArtifactComponent>(otherEnt))
            return;

        component.Contacts.Add(otherEnt);

        if (component.Console != null)
            UpdateUserInterface(component.Console.Value);
    }

    private void OnEndCollide(EntityUid uid, ArtifactAnalyzerComponent component, ref EndCollideEvent args)
    {
        var otherEnt = args.OtherFixture.Body.Owner;

        if (!HasComp<ArtifactComponent>(otherEnt))
            return;
        component.Contacts.Remove(otherEnt);

        if (component.Console != null && Exists(component.Console))
            UpdateUserInterface(component.Console.Value);
    }

    private void OnAnalyzeStart(EntityUid uid, ActiveArtifactAnalyzerComponent component, ComponentStartup args)
    {
        if (TryComp<ApcPowerReceiverComponent>(uid, out var powa))
            powa.NeedsPower = true;

        if (TryComp<AmbientSoundComponent>(uid, out var ambientSound))
        {
            ambientSound.Enabled = true;
            Dirty(ambientSound);
        }
    }

    private void OnAnalyzeEnd(EntityUid uid, ActiveArtifactAnalyzerComponent component, ComponentShutdown args)
    {
        if (TryComp<ApcPowerReceiverComponent>(uid, out var powa))
            powa.NeedsPower = false;

        if (TryComp<AmbientSoundComponent>(uid, out var ambientSound))
        {
            ambientSound.Enabled = false;
            Dirty(ambientSound);
        }
    }

    private void OnPowerChanged(EntityUid uid, ActiveArtifactAnalyzerComponent component, ref PowerChangedEvent args)
    {
        if (!args.Powered)
            CancelScan(component.Artifact);
    }
}

