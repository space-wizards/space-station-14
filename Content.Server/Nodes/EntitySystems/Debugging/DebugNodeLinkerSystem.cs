using Content.Server.Nodes.Components;
using Content.Server.Nodes.Components.Debugging;
using Content.Server.Popups;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Nodes;
using Content.Shared.Verbs;
using Robust.Shared.Audio;

namespace Content.Server.Nodes.EntitySystems.Debugging;

/// <summary>
/// The system that handles the behaviour of the debug linker tool.
/// </summary>
public sealed partial class DebugNodeLinkerSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audioSys = default!;
    [Dependency] private readonly NodeGraphSystem _nodeGraphSys = default!;
    [Dependency] private readonly PopupSystem _popupSys = default!;
    private EntityQuery<GraphNodeComponent> _nodeQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        _nodeQuery = GetEntityQuery<GraphNodeComponent>();

        SubscribeLocalEvent<DebugNodeLinkerComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<DebugNodeLinkerComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
        SubscribeLocalEvent<DebugNodeLinkerComponent, AfterInteractEvent>(AfterInteract);
        SubscribeLocalEvent<DebugNodeLinkerComponent, UseInHandEvent>(OnUseInHand);
    }

    /// <summary>
    /// Adds the currently active mode and edge flags of the linker tool to the examine text.
    /// </summary>
    private void OnExamined(EntityUid uid, DebugNodeLinkerComponent comp, ExaminedEvent args)
    {
        args.PushText($"Currently {comp.Mode}ing nodes.");
        args.PushText($"Resulting edges {((comp.Flags & EdgeFlags.NoMerge) == EdgeFlags.None ? "allow" : "disallow")} merging.");
    }

    /// <summary>
    /// Adds a verb to swap whether edges the linker tool creates can be merged across.
    /// </summary>
    private void OnGetVerbs(EntityUid uid, DebugNodeLinkerComponent comp, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        args.Verbs.Add(new AlternativeVerb()
        {
            Text = $"{((comp.Flags & EdgeFlags.NoMerge) != EdgeFlags.None ? "enable" : "disable")} merging",
            Act = () =>
            {
                comp.Flags ^= EdgeFlags.NoMerge;
                _audioSys.PlayPredicted(comp.ModeSound, uid, args.User, AudioParams.Default.WithVariation(0.125f).WithVolume(8f));
            },
        });
    }

    /// <summary>
    /// Handles linking nodes, unlinking nodes, marking nodes for the previous, and prompting node autolinker updates.
    /// </summary>
    private void AfterInteract(EntityUid uid, DebugNodeLinkerComponent comp, AfterInteractEvent args)
    {
        if (args.Handled)
            return;

        if (args.Target is not { } targetNodeId || !_nodeQuery.TryGetComponent(targetNodeId, out var targetNode))
            return;

        args.Handled = true;
        Dirty(uid, comp);

        // Prompt the node to update its automatic edges:
        if (comp.Mode == DebugLinkerMode.Update)
        {
            if ((targetNode.Flags & NodeFlags.Edges) == NodeFlags.None)
                _nodeGraphSys.QueueEdgeUpdate(targetNodeId, targetNode);
            _audioSys.PlayPredicted(comp.UpdateSound, uid, args.User, AudioParams.Default.WithVariation(0.125f).WithVolume(8f));
            _popupSys.PopupClient("updated", targetNodeId, args.User);
            return;
        }

        // Selects the clicked node as the node to connect ot the next clicked node.
        if (comp.Node is not { } savedNodeId || !Exists(savedNodeId) || !_nodeQuery.TryGetComponent(savedNodeId, out var savedNode))
        {
            comp.Node = targetNodeId;
            _audioSys.PlayPredicted(comp.MarkSound, uid, args.User, AudioParams.Default.WithVariation(0.125f).WithVolume(8f));
            _popupSys.PopupClient("marked", targetNodeId, args.User);
            return;
        }

        comp.Node = null;

        // Clear the selected node.
        if (savedNodeId == targetNodeId)
        {
            _audioSys.PlayPredicted(comp.ClearSound, uid, args.User, AudioParams.Default.WithVariation(0.125f).WithVolume(8f));
            _popupSys.PopupClient("unmarked", targetNodeId, args.User);
            return;
        }

        switch (comp.Mode)
        {
            // Adds an edge between the selected node and the clicked node.
            case DebugLinkerMode.Link:
                if (_nodeGraphSys.HasEdge((savedNodeId, savedNode), targetNodeId)
                    ? !_nodeGraphSys.TrySetEdge(savedNodeId, targetNodeId, flags: comp.Flags, node: savedNode, edge: targetNode)
                    : !_nodeGraphSys.TryAddEdge(savedNodeId, targetNodeId, flags: comp.Flags, node: savedNode, edge: targetNode))
                    break;

                _audioSys.PlayPredicted(comp.LinkSound, uid, args.User, AudioParams.Default.WithVariation(0.125f).WithVolume(8f));
                _popupSys.PopupClient("edge created", targetNodeId, args.User);
                return;

            // Removes an edge between the selected node and the clicked node.
            case DebugLinkerMode.Unlink:
                if (!_nodeGraphSys.TryRemoveEdge(savedNodeId, targetNodeId, node: savedNode, edge: targetNode))
                    break;

                _audioSys.PlayPredicted(comp.UnlinkSound, uid, args.User, AudioParams.Default.WithVariation(0.125f).WithVolume(8f));
                _popupSys.PopupClient("edge removed", targetNodeId, args.User);
                return;
        }

        _audioSys.PlayPredicted(comp.FailSound, uid, args.User, AudioParams.Default.WithVariation(0.125f).WithVolume(8f));
    }

    /// <summary>
    /// Handles rotating the linker tool mode.
    /// </summary>
    private void OnUseInHand(EntityUid uid, DebugNodeLinkerComponent comp, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        comp.Mode = comp.Mode.Next();
        comp.Node = null;
        _popupSys.PopupClient($"{comp.Mode}ing", uid, args.User);
        _audioSys.PlayPredicted(comp.ModeSound, uid, args.User, AudioParams.Default.WithVariation(0.125f).WithVolume(8f));
    }
}
