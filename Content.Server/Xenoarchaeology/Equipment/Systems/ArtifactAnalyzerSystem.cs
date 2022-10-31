using System.Linq;
using Content.Server.MachineLinking.Events;
using Content.Server.Research;
using Content.Server.Research.Components;
using Content.Server.UserInterface;
using Content.Server.Xenoarchaeology.Equipment.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts;
using Content.Shared.MachineLinking.Events;
using Content.Shared.Research.Components;
using Content.Shared.Xenoarchaeology.Equipment;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Xenoarchaeology.Equipment.Systems;

public sealed class ArtifactAnalyzerSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<AnalysisConsoleComponent, NewLinkEvent>(OnNewLink);
        SubscribeLocalEvent<AnalysisConsoleComponent, PortDisconnectedEvent>(OnPortDisconnected);

        //ui events
        SubscribeLocalEvent<AnalysisConsoleComponent, ResearchClientServerSelectedMessage>(UpdateUserInterface, after: new []{typeof(ResearchSystem)});
        SubscribeLocalEvent<AnalysisConsoleComponent, ResearchClientServerDeselectedMessage>(UpdateUserInterface, after: new []{typeof(ResearchSystem)});
        SubscribeLocalEvent<AnalysisConsoleComponent, BeforeActivatableUIOpenEvent>(UpdateUserInterface);
        SubscribeLocalEvent<AnalysisConsoleComponent, AnalysisConsoleServerSelectionMessage>(OnServerSelectionMessage);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

    }

    private EntityUid? GetArtifactForAnalysis(EntityUid? uid, ArtifactAnalyzerComponent? component = null)
    {
        if (uid == null)
            return null;

        if (!Resolve(uid.Value, ref component))
            return null;

        var ent = _lookup.GetEntitiesIntersecting(uid.Value,
            LookupFlags.Dynamic | LookupFlags.Sundries | LookupFlags.Approximate);

        var validEnts = ent.Where(HasComp<ArtifactComponent>).ToHashSet();
        return validEnts.FirstOrNull();
    }

    private void UpdateAnalyzer(EntityUid uid, ArtifactAnalyzerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.LastAnalyzedArtifact = GetArtifactForAnalysis(uid, component);

        if (component.LastAnalyzedArtifact == null)
        {
            component.LastAnalyzedCompletion = null;
            component.LastAnalyzedNode = null;
        }
        else if (TryComp<ArtifactComponent>(component.LastAnalyzedArtifact, out var artifact))
        {
            component.LastAnalyzedNode = artifact.CurrentNode;

            if (artifact.NodeTree != null)
            {
                var discoveredNodes = artifact.NodeTree.AllNodes.Count(x => x.Discovered && x.Triggered);
                component.LastAnalyzedCompletion = (float) discoveredNodes / artifact.NodeTree.AllNodes.Count;
            }
        }
    }

    private void OnNewLink(EntityUid uid, AnalysisConsoleComponent component, NewLinkEvent args)
    {
        if (!HasComp<ArtifactAnalyzerComponent>(args.Receiver))
            return;

        component.AnalyzerEntity = args.Receiver;

        UpdateUserInterface(uid, component);
    }

    private void OnPortDisconnected(EntityUid uid, AnalysisConsoleComponent component, PortDisconnectedEvent args)
    {
        if (args.Port == component.ConsolePort)
        {
            component.AnalyzerEntity = null;
        }

        UpdateUserInterface(uid, component);
    }

    private void UpdateUserInterface(EntityUid uid, AnalysisConsoleComponent component, object? _ = null)
    {
        EntityUid? artifact = null;
        ArtifactNode? node = null;
        float? completion = null;
        if (component.AnalyzerEntity != null && TryComp<ArtifactAnalyzerComponent>(component.AnalyzerEntity, out var analyzer))
        {
            //TODO: remove once scanning
            UpdateAnalyzer(component.AnalyzerEntity.Value);

            artifact = analyzer.LastAnalyzedArtifact;
            node = analyzer.LastAnalyzedNode;
            completion = analyzer.LastAnalyzedCompletion;
        }

        var analyzerConnected = component.AnalyzerEntity != null;
        var serverConnected = TryComp<ResearchClientComponent>(uid, out var client) && client.ConnectedToServer;

        var state = new AnalysisConsoleScanUpdateState(artifact, analyzerConnected, serverConnected,
            node?.Id, node?.Depth, node?.Edges.Count, node?.Triggered, node?.Effect.ID, node?.Trigger.ID, completion);
        var bui = _ui.GetUi(uid, ArtifactAnalzyerUiKey.Key);
        _ui.SetUiState(bui, state);
    }

    private void OnServerSelectionMessage(EntityUid uid, AnalysisConsoleComponent component, AnalysisConsoleServerSelectionMessage args)
    {
        _ui.TryOpen(uid, ResearchClientUiKey.Key, (IPlayerSession) args.Session);
        GetArtifactForAnalysis(component.AnalyzerEntity);
    }
}

