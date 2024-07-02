using System.Linq;
using Content.Server.Paper;
using Content.Server.Power.Components;
using Content.Server.Research.Systems;
using Content.Shared.UserInterface;
using Content.Server.Xenoarchaeology.Equipment.Components;
using Content.Server.Xenoarchaeology.Equipment.Systems;
using Content.Server.Xenoarchaeology.XenoArtifacts;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Content.Shared.Audio;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Placeable;
using Content.Shared.Popups;
using Content.Shared.Research.Components;
using Content.Shared.Xenoarchaeology.Equipment;
using Content.Shared.Xenoarchaeology.Equipment.Components;
using Content.Shared.Xenoarchaeology.XenoArtifacts;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Xenoarchaeology.Equipment;

/// <summary>
/// This system is used for managing the artifact analyzer as well as the analysis console.
/// It also hanadles scanning and ui updates for both systems.
/// </summary>
public sealed class ArtifactAnalyzerSystem : SharedArtifactAnalyzerSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedAmbientSoundSystem _ambientSound = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly ResearchSystem _research = default!;
    [Dependency] private readonly MetaDataSystem _metaSystem = default!;
    [Dependency] private readonly TraversalDistorterSystem _traversalDistorter = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActiveScannedArtifactComponent, ArtifactActivatedEvent>(OnArtifactActivated);

        SubscribeLocalEvent<ActiveArtifactAnalyzerComponent, ComponentStartup>(OnAnalyzeStart);
        SubscribeLocalEvent<ActiveArtifactAnalyzerComponent, ComponentShutdown>(OnAnalyzeEnd);
        SubscribeLocalEvent<ActiveArtifactAnalyzerComponent, PowerChangedEvent>(OnPowerChanged);

        SubscribeLocalEvent<AnalysisConsoleComponent, AnalysisConsoleServerSelectionMessage>(OnServerSelectionMessage);
        SubscribeLocalEvent<AnalysisConsoleComponent, AnalysisConsoleScanButtonPressedMessage>(OnScanButton);
        SubscribeLocalEvent<AnalysisConsoleComponent, AnalysisConsolePrintButtonPressedMessage>(OnPrintButton);
        SubscribeLocalEvent<AnalysisConsoleComponent, AnalysisConsoleExtractButtonPressedMessage>(OnExtractButton);
        SubscribeLocalEvent<AnalysisConsoleComponent, AnalysisConsoleBiasButtonPressedMessage>(OnBiasButton);

        SubscribeLocalEvent<AnalysisConsoleComponent, ResearchClientServerSelectedMessage>((e, c, _) => UpdateUserInterface(e, c),
            after: [typeof(ResearchSystem)]);
        SubscribeLocalEvent<AnalysisConsoleComponent, ResearchClientServerDeselectedMessage>((e, c, _) => UpdateUserInterface(e, c),
            after: [typeof(ResearchSystem)]);
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
    }

    private void UpdateUserInterface(EntityUid uid, AnalysisConsoleComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        return;

    }

    /// <summary>
    /// opens the server selection menu.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="component"></param>
    /// <param name="args"></param>
    private void OnServerSelectionMessage(EntityUid uid, AnalysisConsoleComponent component, AnalysisConsoleServerSelectionMessage args)
    {
        _ui.OpenUi(uid, ResearchClientUiKey.Key, args.Actor);
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
    }

    private void OnPrintButton(EntityUid uid, AnalysisConsoleComponent component, AnalysisConsolePrintButtonPressedMessage args)
    {

    }

    private FormattedMessage? GetArtifactScanMessage(ArtifactAnalyzerComponent component)
    {
        var msg = new FormattedMessage();

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

        // var artifact = GetArtifactForAnalysis(component.AnalyzerEntity);
        // if (artifact == null)
        //     return;
        //
        // var pointValue = _artifact.GetResearchPointValue(artifact.Value);
        //
        // // no new nodes triggered so nothing to add
        // if (pointValue == 0)
        //     return;
        //
        // _research.ModifyServerPoints(server.Value, pointValue, serverComponent);
        // _artifact.AdjustConsumedPoints(artifact.Value, pointValue);
        //
        // _audio.PlayPvs(component.ExtractSound, component.AnalyzerEntity.Value, AudioParams.Default.WithVolume(2f));
        //
        // _popup.PopupEntity(Loc.GetString("analyzer-artifact-extract-popup"),
        //     component.AnalyzerEntity.Value, PopupType.Large);

        UpdateUserInterface(uid, component);
    }

    private void OnBiasButton(EntityUid uid, AnalysisConsoleComponent component, AnalysisConsoleBiasButtonPressedMessage args)
    {
        if (component.AnalyzerEntity == null)
            return;

        // if (!TryComp<TraversalDistorterComponent>(component.AnalyzerEntity, out var trav))
        //     return;
        //
        // if (!_traversalDistorter.SetState(component.AnalyzerEntity.Value, trav, args.IsDown))
        //     return;

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

