using System.Diagnostics.CodeAnalysis;
using Content.Server.Administration.Logs;
using Content.Server.Electrocution;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.Power.Components;
using Content.Server.Power.Nodes;
using Content.Server.Stack;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.NodeContainer;
using Content.Shared.SubFloor;
using Content.Shared.Tools.Components;
using Content.Shared.Verbs;
using Robust.Shared.Map;
using CableCuttingFinishedEvent = Content.Shared.Tools.Systems.CableCuttingFinishedEvent;
using CableToggleFinishedEvent = Content.Shared.Tools.Systems.CableToggleFinishedEvent;
using SharedToolSystem = Content.Shared.Tools.Systems.SharedToolSystem;

namespace Content.Server.Power.EntitySystems;

public sealed partial class CableSystem : EntitySystem
{
    [Dependency] private ITileDefinitionManager _tileManager = default!;
    [Dependency] private SharedToolSystem _toolSystem = default!;
    [Dependency] private StackSystem _stack = default!;
    [Dependency] private ElectrocutionSystem _electrocutionSystem = default!;
    [Dependency] private NodeContainerSystem _nodeContainer = default!;
    [Dependency] private NodeGroupSystem _nodeGroup = default!;

    public override void Initialize()
    {
        base.Initialize();

        InitializeCablePlacer();

        SubscribeLocalEvent<CableComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<CableComponent, CableCuttingFinishedEvent>(OnCableCut);
        SubscribeLocalEvent<CableComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
        SubscribeLocalEvent<CableComponent, CableToggleFinishedEvent>(OnCableToggle);
        SubscribeLocalEvent<CableComponent, ExaminedEvent>(OnExamined);
        // Shouldn't need re-anchoring.
        SubscribeLocalEvent<CableComponent, AnchorStateChangedEvent>(OnAnchorChanged);
    }

    private void OnExamined(EntityUid uid, CableComponent cable, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (TryGetCableNode(uid, out var node) && !node.Enabled)
            args.PushMarkup(Loc.GetString("cable-system-examine-disconnected"));
    }

    private void OnInteractUsing(EntityUid uid, CableComponent cable, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (cable.CuttingQuality != null)
        {
            args.Handled = _toolSystem.UseTool(args.Used, args.User, uid, cable.CuttingDelay, cable.CuttingQuality, new CableCuttingFinishedEvent());
        }
    }

    // Manually connect/disconnect a cable's powernet node with a cutting tool, without removing the cable.
    private void OnGetVerbs(EntityUid uid, CableComponent cable, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !args.CanComplexInteract || args.Using is not { } used)
            return;

        if (cable.CuttingQuality == null ||
            !TryComp<ToolComponent>(used, out var tool) ||
            !_toolSystem.HasQuality(used, cable.CuttingQuality.Value, tool))
            return;

        if (TryComp<SubFloorHideComponent>(uid, out var subFloor) && subFloor.IsUnderCover)
            return;

        if (!TryGetCableNode(uid, out _))
            return;

        var user = args.User;
        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("cable-system-verb-toggle"),
            Impact = LogImpact.Medium,
            DoContactInteraction = true,
            Act = () => _toolSystem.UseTool(used, user, uid, cable.CuttingDelay, cable.CuttingQuality, new CableToggleFinishedEvent()),
        });
    }

    private void OnCableToggle(EntityUid uid, CableComponent cable, CableToggleFinishedEvent args)
    {
        if (args.Cancelled)
            return;

        if (!TryGetCableNode(uid, out var node))
            return;

        node.Enabled = !node.Enabled;
        _nodeGroup.QueueReflood(node);

        _adminLogger.Add(LogType.CableCut, LogImpact.Medium,
            $"The {ToPrettyString(uid)} at {Transform(uid).Coordinates} was {(node.Enabled ? "reconnected" : "disconnected")} by {ToPrettyString(args.User)}.");
    }

    private bool TryGetCableNode(EntityUid uid, [NotNullWhen(true)] out CableNode? node)
    {
        node = null;
        return TryComp<NodeContainerComponent>(uid, out var nodeContainer)
            && _nodeContainer.TryGetNode(nodeContainer, "power", out node);
    }

    private void OnCableCut(EntityUid uid, CableComponent cable, DoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        var xform = Transform(uid);
        var ev = new CableAnchorStateChangedEvent(xform);
        RaiseLocalEvent(uid, ref ev);

        if (_electrocutionSystem.TryDoElectrifiedAct(uid, args.User))
            return;

        _adminLogger.Add(LogType.CableCut, LogImpact.High, $"The {ToPrettyString(uid)} at {xform.Coordinates} was cut by {ToPrettyString(args.User)}.");

        Spawn(cable.CableDroppedOnCutPrototype, xform.Coordinates);
        QueueDel(uid);
    }

    private void OnAnchorChanged(EntityUid uid, CableComponent cable, ref AnchorStateChangedEvent args)
    {
        var ev = new CableAnchorStateChangedEvent(args.Transform, args.Detaching);
        RaiseLocalEvent(uid, ref ev);

        if (args.Anchored)
            return; // huh? it wasn't anchored?

        // anchor state can change as a result of deletion (detach to null).
        // We don't want to spawn an entity when deleted.
        if (TerminatingOrDeleted(uid))
            return;

        // This entity should not be un-anchorable. But this can happen if the grid-tile is deleted (RCD, explosion,
        // etc). In that case: behave as if the cable had been cut.
        Spawn(cable.CableDroppedOnCutPrototype, Transform(uid).Coordinates);
        QueueDel(uid);
    }
}
