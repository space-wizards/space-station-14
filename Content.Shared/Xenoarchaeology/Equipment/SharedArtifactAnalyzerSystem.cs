using System.Diagnostics.CodeAnalysis;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Placeable;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Equipment.Components;

namespace Content.Shared.Xenoarchaeology.Equipment;

/// <summary>
/// This system is used for managing the artifact analyzer as well as the analysis console.
/// It also handles scanning and ui updates for both systems.
/// </summary>
public abstract class SharedArtifactAnalyzerSystem : EntitySystem
{
    [Dependency] private readonly SharedPowerReceiverSystem _powerReceiver = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ArtifactAnalyzerComponent, ItemPlacedEvent>(OnItemPlaced);
        SubscribeLocalEvent<ArtifactAnalyzerComponent, ItemRemovedEvent>(OnItemRemoved);
        SubscribeLocalEvent<ArtifactAnalyzerComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<AnalysisConsoleComponent, NewLinkEvent>(OnNewLink);
        SubscribeLocalEvent<AnalysisConsoleComponent, PortDisconnectedEvent>(OnPortDisconnected);
    }

    private void OnItemPlaced(Entity<ArtifactAnalyzerComponent> ent, ref ItemPlacedEvent args)
    {
        ent.Comp.CurrentArtifact = args.OtherEntity;
        Dirty(ent);
    }

    private void OnItemRemoved(Entity<ArtifactAnalyzerComponent> ent, ref ItemRemovedEvent args)
    {
        if (args.OtherEntity != ent.Comp.CurrentArtifact)
            return;

        ent.Comp.CurrentArtifact = null;
        Dirty(ent);
    }

    private void OnMapInit(Entity<ArtifactAnalyzerComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<DeviceLinkSinkComponent>(ent, out var sink))
            return;

        foreach (var source in sink.LinkedSources)
        {
            if (!TryComp<AnalysisConsoleComponent>(source, out var analysis))
                continue;

            analysis.AnalyzerEntity = GetNetEntity(ent);
            ent.Comp.Console = source;
            Dirty(source, analysis);
            Dirty(ent);
            break;
        }
    }

    private void OnNewLink(Entity<AnalysisConsoleComponent> ent, ref NewLinkEvent args)
    {
        if (!TryComp<ArtifactAnalyzerComponent>(args.Sink, out var analyzer))
            return;

        ent.Comp.AnalyzerEntity = GetNetEntity(args.Sink);
        analyzer.Console = ent;
        Dirty(args.Sink, analyzer);
        Dirty(ent);
    }

    private void OnPortDisconnected(Entity<AnalysisConsoleComponent> ent, ref PortDisconnectedEvent args)
    {
        var analyzerNetEntity = ent.Comp.AnalyzerEntity;
        if (args.Port != ent.Comp.LinkingPort || analyzerNetEntity == null)
            return;

        var analyzerEntityUid = GetEntity(analyzerNetEntity);
        if (TryComp<ArtifactAnalyzerComponent>(analyzerEntityUid, out var analyzer))
        {
            analyzer.Console = null;
            Dirty(analyzerEntityUid.Value, analyzer);
        }

        ent.Comp.AnalyzerEntity = null;
        Dirty(ent);
    }

    public bool TryGetAnalyzer(Entity<AnalysisConsoleComponent> ent, [NotNullWhen(true)] out Entity<ArtifactAnalyzerComponent>? analyzer)
    {
        analyzer = null;

        var consoleEnt = ent.Owner;
        if (!_powerReceiver.IsPowered(consoleEnt))
            return false;

        var analyzerUid = GetEntity(ent.Comp.AnalyzerEntity);
        if (!TryComp<ArtifactAnalyzerComponent>(analyzerUid, out var analyzerComp))
            return false;

        if (!_powerReceiver.IsPowered(analyzerUid.Value))
            return false;

        analyzer = (analyzerUid.Value, analyzerComp);
        return true;
    }

    public bool TryGetArtifactFromConsole(Entity<AnalysisConsoleComponent> ent, [NotNullWhen(true)] out Entity<XenoArtifactComponent>? artifact)
    {
        artifact = null;

        if (!TryGetAnalyzer(ent, out var analyzer))
            return false;

        if (!TryComp<XenoArtifactComponent>(analyzer.Value.Comp.CurrentArtifact, out var comp))
            return false;

        artifact = (analyzer.Value.Comp.CurrentArtifact.Value, comp);
        return true;
    }

    public bool TryGetAnalysisConsole(Entity<ArtifactAnalyzerComponent> ent, [NotNullWhen(true)] out Entity<AnalysisConsoleComponent>? analysisConsole)
    {
        analysisConsole = null;

        if (!TryComp<AnalysisConsoleComponent>(ent.Comp.Console, out var consoleComp))
            return false;

        analysisConsole = (ent.Comp.Console.Value, consoleComp);
        return true;
    }
}
