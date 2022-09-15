using Content.Server.Administration.Logs;
using Content.Server.Electrocution;
using Content.Server.Examine;
using Content.Server.NodeContainer;
using Content.Server.Power.Components;
using Content.Server.Power.NodeGroups;
using Content.Server.Stack;
using Content.Server.Tools;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Verbs;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Server.Power.EntitySystems;

public sealed partial class CableSystem : EntitySystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly ITileDefinitionManager _tileManager = default!;
    [Dependency] private readonly ToolSystem _toolSystem = default!;
    [Dependency] private readonly StackSystem _stack = default!;
    [Dependency] private readonly ElectrocutionSystem _electrocutionSystem = default!;
    [Dependency] private readonly IAdminLogManager _adminLogs = default!;
    [Dependency] private readonly ExamineSystemShared _examineSystem = default!;
    [Dependency] private readonly SharedVerbSystem _verbSystem = default!;
    [Dependency] private readonly PowerNetSystem _pnSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        InitializeCablePlacer();

        SubscribeLocalEvent<CableComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<CableComponent, CuttingFinishedEvent>(OnCableCut);
        // Shouldn't need re-anchoring.
        SubscribeLocalEvent<CableComponent, AnchorStateChangedEvent>(OnAnchorChanged);
    }

    private void OnInteractUsing(EntityUid uid, CableComponent cable, InteractUsingEvent args)
    {
        if (args.Handled || _examineSystem.IsInDetailsRange(args.User, args.Target))
            return;

        if (_toolSystem.HasQuality(args.Used, "Pulsing"))
        {
            var verb = new ExamineVerb
            {
                Message = Loc.GetString("cable-multitool-system-verb-tooltip"),
                Text = Loc.GetString("cable-multitool-system-verb-name"),
                Category = VerbCategory.Examine,
                IconTexture = "/Textures/Interface/VerbIcons/zap.svg.192dpi.png",
                Act = () =>
                {
                    Logger.DebugS("debug", "Verb acted out, apparently.");
                    var markup = FormattedMessage.FromMarkup(GenerateCableMarkup(uid));
                    _examineSystem.SendExamineTooltip(args.User, uid, markup, true, false);
                }
            };
            Logger.DebugS("debug", "Verb created, executing it...");
            _verbSystem.ExecuteVerb(verb, args.User, args.Target, true);
            Logger.DebugS("debug", "Verb executed.");

            //var markup = FormattedMessage.FromMarkup(GenerateCableMarkup(uid));
            //_examineSystem.SendExamineTooltip(args.User, uid, markup, true, false);

            args.Handled = true;
            return;
        }

        var ev = new CuttingFinishedEvent(args.User);
        _toolSystem.UseTool(args.Used, args.User, uid, 0, cable.CuttingDelay, new[] { cable.CuttingQuality }, doAfterCompleteEvent: ev, doAfterEventTarget: uid);
        args.Handled = true;
    }
    private string GenerateCableMarkup(EntityUid uid, NodeContainerComponent? nodeContainer = null)
    {
        if (!Resolve(uid, ref nodeContainer))
            return Loc.GetString("cable-multitool-system-internal-error-missing-component");

        foreach (var node in nodeContainer.Nodes)
        {
            if (!(node.Value.NodeGroup is IBasePowerNet))
                continue;
            var p = (IBasePowerNet) node.Value.NodeGroup;
            var ps = _pnSystem.GetNetworkStatistics(p.NetworkNode);

            float storageRatio = ps.InStorageCurrent / Math.Max(ps.InStorageMax, 1.0f);
            float outStorageRatio = ps.OutStorageCurrent / Math.Max(ps.OutStorageMax, 1.0f);
            return Loc.GetString("cable-multitool-system-statistics",
                ("supplyc", ps.SupplyCurrent),
                ("supplyb", ps.SupplyBatteries),
                ("supplym", ps.SupplyTheoretical),
                ("consumption", ps.Consumption),
                ("storagec", ps.InStorageCurrent),
                ("storager", storageRatio),
                ("storagem", ps.InStorageMax),
                ("storageoc", ps.OutStorageCurrent),
                ("storageor", outStorageRatio),
                ("storageom", ps.OutStorageMax)
            );
        }
        return Loc.GetString("cable-multitool-system-internal-error-no-power-node");
    }

    private void OnCableCut(EntityUid uid, CableComponent cable, CuttingFinishedEvent args)
    {
        if (_electrocutionSystem.TryDoElectrifiedAct(uid, args.User))
            return;

        _adminLogs.Add(LogType.CableCut, LogImpact.Medium, $"The {ToPrettyString(uid)} at {Transform(uid).Coordinates} was cut by {ToPrettyString(args.User)}.");

        Spawn(cable.CableDroppedOnCutPrototype, Transform(uid).Coordinates);
        QueueDel(uid);
    }

    private void OnAnchorChanged(EntityUid uid, CableComponent cable, ref AnchorStateChangedEvent args)
    {
        if (args.Anchored)
            return; // huh? it wasn't anchored?

        // anchor state can change as a result of deletion (detach to null).
        // We don't want to spawn an entity when deleted.
        if (!TryLifeStage(uid, out var life) || life >= EntityLifeStage.Terminating)
            return;

        // This entity should not be un-anchorable. But this can happen if the grid-tile is deleted (RCD, explosion,
        // etc). In that case: behave as if the cable had been cut.
        Spawn(cable.CableDroppedOnCutPrototype, Transform(uid).Coordinates);
        QueueDel(uid);
    }
}

public sealed class CuttingFinishedEvent : EntityEventArgs
{
    public EntityUid User;

    public CuttingFinishedEvent(EntityUid user)
    {
        User = user;
    }
}
