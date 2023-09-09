using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Nodes.Components;
using Content.Shared.Nodes.EntitySystems;
using Content.Shared.Popups;
using Robust.Shared.Audio;

namespace Content.Shared.Nodes.Debugging;

public sealed partial class DebugNodeLinkerSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audioSys = default!;
    [Dependency] private readonly SharedNodeGraphSystem _nodeGraphSys = default!;
    [Dependency] private readonly SharedPopupSystem _popupSys = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DebugNodeLinkerComponent, AfterInteractEvent>(AfterInteract);
        SubscribeLocalEvent<DebugNodeLinkerComponent, UseInHandEvent>(OnUseInHand);
    }

    private void AfterInteract(EntityUid uid, DebugNodeLinkerComponent comp, AfterInteractEvent args)
    {
        if (args.Handled)
            return;

        if (args.Target is not { } targetNodeId || !TryComp<GraphNodeComponent>(targetNodeId, out var targetNode))
            return;

        args.Handled = true;
        Dirty(uid, comp);

        // Selects the clicked node as the node to connect ot the next clicked node.
        if (comp.Node is not { } savedNodeId || !Exists(savedNodeId) || !TryComp<GraphNodeComponent>(savedNodeId, out var savedNode))
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
            case true:
                if (!_nodeGraphSys.AddEdge(savedNodeId, targetNodeId, node: savedNode, edge: targetNode))
                    return;

                _audioSys.PlayPredicted(comp.LinkSound, uid, args.User, AudioParams.Default.WithVariation(0.125f).WithVolume(8f));
                _popupSys.PopupClient("edge created", targetNodeId, args.User);
                break;

            // Removes an edge between the selected node and the clicked node.
            case false:
                if (!_nodeGraphSys.RemoveEdge(savedNodeId, targetNodeId, node: savedNode, edge: targetNode))
                    return;

                _audioSys.PlayPredicted(comp.UnlinkSound, uid, args.User, AudioParams.Default.WithVariation(0.125f).WithVolume(8f));
                _popupSys.PopupClient("edge removed", targetNodeId, args.User);
                break;
        }
    }

    private void OnUseInHand(EntityUid uid, DebugNodeLinkerComponent comp, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        comp.Mode = !comp.Mode;
        _popupSys.PopupClient(comp.Mode ? "linking" : "unlinking", uid, args.User);
    }
}
