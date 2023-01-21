using Content.Server.Anomaly.Components;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Anomaly;
using Content.Shared.CCVar;
using Content.Shared.Materials;
using Robust.Shared.Map.Components;

namespace Content.Server.Anomaly;

/// <summary>
/// This handles anomalous vessel as well as
/// the calculations for how many points they
/// should produce.
/// </summary>
public sealed partial class AnomalySystem
{
    private void InitializeGenerator()
    {
        SubscribeLocalEvent<AnomalyGeneratorComponent, BoundUIOpenedEvent>(OnGeneratorBUIOpened);
        SubscribeLocalEvent<AnomalyGeneratorComponent, MaterialAmountChangedEvent>(OnGeneratorMaterialAmountChanged);
        SubscribeLocalEvent<AnomalyGeneratorComponent, AnomalyGeneratorGenerateButtonPressedEvent>(OnGenerateButtonPressed);
        SubscribeLocalEvent<AnomalyGeneratorComponent, PowerChangedEvent>(OnGeneratorPowerChanged);
    }

    private void OnGeneratorPowerChanged(EntityUid uid, AnomalyGeneratorComponent component, ref PowerChangedEvent args)
    {
        _ambient.SetAmbience(uid, args.Powered);
    }

    private void OnGeneratorBUIOpened(EntityUid uid, AnomalyGeneratorComponent component, BoundUIOpenedEvent args)
    {
        UpdateGeneratorUi(uid, component);
    }

    private void OnGeneratorMaterialAmountChanged(EntityUid uid, AnomalyGeneratorComponent component, ref MaterialAmountChangedEvent args)
    {
        UpdateGeneratorUi(uid, component);
    }

    private void OnGenerateButtonPressed(EntityUid uid, AnomalyGeneratorComponent component, AnomalyGeneratorGenerateButtonPressedEvent args)
    {
        TryGeneratorCreateAnomaly(uid, component);
    }

    public void UpdateGeneratorUi(EntityUid uid, AnomalyGeneratorComponent component)
    {
        var materialAmount = _material.GetMaterialAmount(uid, component.RequiredMaterial);

        var state = new AnomalyGeneratorUserInterfaceState(component.CooldownEndTime, materialAmount, component.MaterialPerAnomaly);
        _ui.TrySetUiState(uid, AnomalyGeneratorUiKey.Key, state);
    }

    public void TryGeneratorCreateAnomaly(EntityUid uid, AnomalyGeneratorComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!this.IsPowered(uid, EntityManager))
            return;

        if (Timing.CurTime < component.CooldownEndTime)
            return;

        var grid = Transform(uid).GridUid;
        if (grid == null)
            return;

        if (!_material.TryChangeMaterialAmount(uid, component.RequiredMaterial, -component.MaterialPerAnomaly))
            return;

        SpawnOnRandomGridLocation(grid.Value, component.SpawnerPrototype);
        component.CooldownEndTime = Timing.CurTime + component.CooldownLength;
        UpdateGeneratorUi(uid, component);
    }

    public void SpawnOnRandomGridLocation(EntityUid grid, string toSpawn)
    {
        if (!TryComp<MapGridComponent>(grid, out var gridComp))
            return;

        var xform = Transform(grid);

        var targetCoords = xform.Coordinates;
        var gridBounds = gridComp.LocalAABB;
        gridBounds.Scale(_configuration.GetCVar(CCVars.AnomalyGenerationGridBoundsScale));

        for (var i = 0; i < 25; i++)
        {
            var randomX = Random.Next((int) gridBounds.Left, (int) gridBounds.Right);
            var randomY = Random.Next((int) gridBounds.Bottom, (int)gridBounds.Top);

            var tile = new Vector2i(randomX, randomY);
            if (_atmosphere.IsTileSpace(grid, xform.MapUid, tile,
                    mapGridComp: gridComp) || _atmosphere.IsTileAirBlocked(grid, tile, mapGridComp: gridComp))
            {
                continue;
            }

            targetCoords = gridComp.GridTileToLocal(tile);
            break;
        }

        Spawn(toSpawn, targetCoords);
    }
}
