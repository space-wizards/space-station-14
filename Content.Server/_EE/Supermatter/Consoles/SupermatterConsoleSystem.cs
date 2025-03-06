using Content.Shared._EE.CCVar;
using Content.Shared._EE.Supermatter.Components;
using Content.Shared._EE.Supermatter.Consoles;
using Content.Shared.Atmos;
using Content.Shared.Radiation.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using System.Linq;

namespace Content.Server._EE.Supermatter.Console.Systems;

public sealed class SupermatterConsoleSystem : SharedSupermatterConsoleSystem
{
    [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Console events
        SubscribeLocalEvent<SupermatterConsoleComponent, ComponentInit>(OnConsoleInit);
        SubscribeLocalEvent<SupermatterConsoleComponent, EntParentChangedMessage>(OnConsoleParentChanged);
        SubscribeLocalEvent<SupermatterConsoleComponent, SupermatterConsoleFocusChangeMessage>(OnFocusChangedMessage);

        // Grid events
        SubscribeLocalEvent<GridSplitEvent>(OnGridSplit);
    }

    #region Event Handling

    private void OnConsoleInit(EntityUid uid, SupermatterConsoleComponent component, ComponentInit args)
    {
        InitalizeConsole(uid, component);
    }

    private void OnConsoleParentChanged(EntityUid uid, SupermatterConsoleComponent component, EntParentChangedMessage args)
    {
        InitalizeConsole(uid, component);
    }

    private void OnFocusChangedMessage(EntityUid uid, SupermatterConsoleComponent component, SupermatterConsoleFocusChangeMessage args)
    {
        component.FocusSupermatter = args.FocusSupermatter;
        Dirty(uid, component);
    }

    private void OnGridSplit(ref GridSplitEvent args)
    {
        // Collect grids
        var allGrids = args.NewGrids.ToList();

        if (!allGrids.Contains(args.Grid))
            allGrids.Add(args.Grid);

        // Update supermatter monitoring consoles that stand on an updated grid
        var query = AllEntityQuery<SupermatterConsoleComponent, TransformComponent>();
        while (query.MoveNext(out var ent, out var entConsole, out var entXform))
        {
            if (entXform.GridUid == null)
                continue;

            if (!allGrids.Contains(entXform.GridUid.Value))
                continue;

            InitalizeConsole(ent, entConsole);
        }
    }

    #endregion

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Keep a list of UI entries for each gridUid, in case multiple consoles stand on the same grid
        var supermatterEntriesForEachGrid = new Dictionary<EntityUid, SupermatterConsoleEntry[]>();

        var query = AllEntityQuery<SupermatterConsoleComponent, TransformComponent>();
        while (query.MoveNext(out var ent, out var entConsole, out var entXform))
        {
            if (entXform?.GridUid == null)
                continue;

            // Make a list of supermatter state data for all the supermatters on the grid
            if (!supermatterEntriesForEachGrid.TryGetValue(entXform.GridUid.Value, out var supermatterEntries))
            {
                supermatterEntries = GetSupermatterStateData(entXform.GridUid.Value).ToArray();
                supermatterEntriesForEachGrid[entXform.GridUid.Value] = supermatterEntries;
            }

            // Determine the highest level of supermatter status for the console
            var highestStatus = SupermatterStatusType.Inactive;

            foreach (var entry in supermatterEntries)
            {
                var status = entry.EntityStatus;

                if (status > highestStatus)
                    highestStatus = status;
            }

            // Update the appearance of the console based on the highest recorded level of supermatter status
            if (TryComp<AppearanceComponent>(ent, out var entAppearance))
                _appearance.SetData(ent, SupermatterConsoleVisuals.ComputerLayerScreen, (int)highestStatus, entAppearance);

            // If the console UI is open, send UI data to each subscribed session
            UpdateUIState(ent, supermatterEntries, entConsole, entXform);
        }
    }

    private void UpdateUIState(EntityUid uid, SupermatterConsoleEntry[] supermatterStateData, SupermatterConsoleComponent component, TransformComponent xform)
    {
        if (!_userInterfaceSystem.IsUiOpen(uid, SupermatterConsoleUiKey.Key))
            return;

        var gridUid = xform.GridUid!.Value;

        // Gathering remaining data to be send to the client
        var focusSupermatterData = GetFocusSupermatterData(GetEntity(component.FocusSupermatter), gridUid);

        // Set the UI state
        _userInterfaceSystem.SetUiState(uid, SupermatterConsoleUiKey.Key,
            new SupermatterConsoleBoundInterfaceState(supermatterStateData, focusSupermatterData));
    }

    private List<SupermatterConsoleEntry> GetSupermatterStateData(EntityUid gridUid)
    {
        var supermatterStateData = new List<SupermatterConsoleEntry>();

        var querySupermatters = AllEntityQuery<SupermatterComponent, TransformComponent>();
        while (querySupermatters.MoveNext(out var ent, out var entSupermatter, out var entXform))
        {
            if (entXform.GridUid != gridUid)
                continue;

            if (!entXform.Anchored)
                continue;

            // Create entry
            var netEnt = GetNetEntity(ent);

            var entry = new SupermatterConsoleEntry(netEnt, MetaData(ent).EntityName, entSupermatter.Status);

            supermatterStateData.Add(entry);
        }

        return supermatterStateData;
    }

    private SupermatterFocusData? GetFocusSupermatterData(EntityUid? focusSupermatter, EntityUid gridUid)
    {
        if (focusSupermatter == null)
            return null;

        if (!TryComp(focusSupermatter.Value, out TransformComponent? focusSupermatterXform))
            return null;

        if (!focusSupermatterXform.Anchored ||
            focusSupermatterXform.GridUid != gridUid)
            return null;

        if (!TryComp<SupermatterComponent>(focusSupermatter.Value, out var sm))
            return null;

        if (!TryComp<RadiationSourceComponent>(focusSupermatter.Value, out var radiationComp))
            return null;

        var gases = GasMixture.SpaceGas;
        if (sm.GasStorage != null)
            gases = sm.GasStorage;

        var tempThreshold = Atmospherics.T0C + _config.GetCVar(EECCVars.SupermatterHeatPenaltyThreshold);

        return new SupermatterFocusData(
            GetNetEntity(focusSupermatter.Value),
            gases,
            GetIntegrity(sm),
            GetHeatHealing(sm),
            sm.Power,
            sm.PowerLoss,
            radiationComp.Intensity,
            tempThreshold * sm.DynamicHeatResistance,
            sm.HeatModifier,
            sm.GasHeatModifier,
            sm.GasEfficiency * 100);
    }

    private static float GetIntegrity(SupermatterComponent sm)
    {
        var integrity = sm.Damage / sm.DamageDelaminationPoint;
        integrity = (float)Math.Round(100 - integrity * 100, 2);
        integrity = integrity < 0 ? 0 : integrity;
        return integrity;
    }

    private static float GetHeatHealing(SupermatterComponent sm)
    {
        var heatHealing = sm.HeatHealing / sm.DamageDelaminationPoint;
        heatHealing = (float)Math.Round(heatHealing * 100, 2);
        heatHealing = heatHealing > 0 ? 0 : heatHealing;
        return heatHealing;
    }

    private void InitalizeConsole(EntityUid uid, SupermatterConsoleComponent component)
    {
        var xform = Transform(uid);

        if (xform.GridUid == null)
            return;

        Dirty(uid, component);
    }
}
