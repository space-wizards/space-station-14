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

        SubscribeLocalEvent<ActiveArtifactAnalyzerComponent, PowerChangedEvent>(OnPowerChanged);

        SubscribeLocalEvent<AnalysisConsoleComponent, AnalysisConsoleExtractButtonPressedMessage>(OnExtractButton);

        SubscribeLocalEvent<AnalysisConsoleComponent, ResearchClientServerSelectedMessage>((e, c, _) => UpdateUserInterface(e, c),
            after: [typeof(ResearchSystem)]);
        SubscribeLocalEvent<AnalysisConsoleComponent, ResearchClientServerDeselectedMessage>((e, c, _) => UpdateUserInterface(e, c),
            after: [typeof(ResearchSystem)]);
        SubscribeLocalEvent<AnalysisConsoleComponent, BeforeActivatableUIOpenEvent>((e, c, _) => UpdateUserInterface(e, c));
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

    private void UpdateUserInterface(EntityUid uid, AnalysisConsoleComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        return;

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

    private void OnPowerChanged(EntityUid uid, ActiveArtifactAnalyzerComponent active, ref PowerChangedEvent args)
    {

    }
}

