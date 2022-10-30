using System.Linq;
using Content.Server.MachineLinking.Events;
using Content.Server.UserInterface;
using Content.Server.Xenoarchaeology.Equipment.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts;
using Content.Shared.MachineLinking.Events;
using Content.Shared.Physics;
using Content.Shared.Research.Components;
using Content.Shared.Xenoarchaeology.Equipment;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Physics.Components;
using Robust.Shared.Utility;

namespace Content.Server.Xenoarchaeology.Equipment.Systems;

public sealed class ArtifactAnalyzerSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly PhysicsSystem _physics = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<AnalysisConsoleComponent, NewLinkEvent>(OnNewLink);
        SubscribeLocalEvent<AnalysisConsoleComponent, PortDisconnectedEvent>(OnPortDisconnected);

        //ui events
        SubscribeLocalEvent<AnalysisConsoleComponent, BeforeActivatableUIOpenEvent>((e,c,_) => UpdateUserInterface(e,c));
        SubscribeLocalEvent<AnalysisConsoleComponent, AnalysisConsoleServerSelectionMessage>(OnServerSelectionMessage);
    }

    private EntityUid? GetArtifactForAnalysis(EntityUid? uid, ArtifactAnalyzerComponent? component = null)
    {
        if (uid == null)
            return null;

        if (!Resolve(uid.Value, ref component))
            return null;

        //TODO: this doesn't allways get everything. idk why
        var ent = _physics.GetEntitiesIntersectingBody(uid.Value, (int) CollisionGroup.AllMask);

        var validEnts = ent.Where(HasComp<ArtifactComponent>).ToHashSet();
        return validEnts.FirstOrNull();
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

    private void UpdateUserInterface(EntityUid uid, AnalysisConsoleComponent component)
    {
        var currentArtifact = GetArtifactForAnalysis(component.AnalyzerEntity);

        var state = new AnalysisConsoleScanUpdateState(currentArtifact,
            component.AnalyzerEntity != null);

        var bui = _ui.GetUi(uid, ArtifactAnalzyerUiKey.Key);
        _ui.SetUiState(bui, state);
    }

    private void OnServerSelectionMessage(EntityUid uid, AnalysisConsoleComponent component, AnalysisConsoleServerSelectionMessage args)
    {
        _ui.TryOpen(uid, ResearchClientUiKey.Key, (IPlayerSession) args.Session);
        GetArtifactForAnalysis(component.AnalyzerEntity);
    }
}

