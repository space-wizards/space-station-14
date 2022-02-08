using Content.Server.Administration.Logs;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Power.Components;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using Content.Shared.Rounding;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Content.Server.PowerCell;

public class PowerCellSystem : SharedPowerCellSystem
{
    [Dependency] private readonly SolutionContainerSystem _solutionsSystem = default!;
    [Dependency] private readonly ExplosionSystem _explosionSystem = default!;
    [Dependency] private readonly AdminLogSystem _logSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PowerCellComponent, ChargeChangedEvent>(OnChargeChanged);
        SubscribeLocalEvent<PowerCellComponent, SolutionChangedEvent>(OnSolutionChange);

        SubscribeLocalEvent<PowerCellComponent, ExaminedEvent>(OnCellExamined);
    }

    private void OnChargeChanged(EntityUid uid, PowerCellComponent component, ChargeChangedEvent args)
    {
        if (component.IsRigged)
        {
            Explode(uid);
            return;
        }

        if (!TryComp(uid, out BatteryComponent? battery))
            return;

        if (!TryComp(uid, out AppearanceComponent? appearance))
            return;

        var frac = battery.CurrentCharge / battery.MaxCharge;
        var level = (byte) ContentHelpers.RoundToNearestLevels(frac, 1, PowerCellComponent.PowerCellVisualsLevels);
        appearance.SetData(PowerCellVisuals.ChargeLevel, level);

        // If this power cell is inside a cell-slot, inform that entity that the power has changed (for updating visuals n such).
        if (_containerSystem.TryGetContainingContainer(uid, out var container)
            && TryComp(container.Owner, out PowerCellSlotComponent? slot)
            && slot.CellSlot.Item == uid)
        {
            RaiseLocalEvent(container.Owner, new PowerCellChangedEvent(false), false);
        }
    }

    private void Explode(EntityUid uid, BatteryComponent? battery = null)
    {
        _logSystem.Add(LogType.Explosion, LogImpact.High, $"Sabotaged power cell {ToPrettyString(uid)} is exploding");

        if (!Resolve(uid, ref battery))
            return;

        var heavy = (int) Math.Ceiling(Math.Sqrt(battery.CurrentCharge) / 60);
        var light = (int) Math.Ceiling(Math.Sqrt(battery.CurrentCharge) / 30);

        _explosionSystem.SpawnExplosion(uid, 0, heavy, light, light * 2);
        QueueDel(uid);
    }

    public bool TryGetBatteryFromSlot(EntityUid uid, [NotNullWhen(true)] out BatteryComponent? battery, PowerCellSlotComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
        {
            battery = null;
            return false;
        }

        return TryComp(component.CellSlot.Item, out battery);
    }

    private void OnSolutionChange(EntityUid uid, PowerCellComponent component, SolutionChangedEvent args)
    {
        component.IsRigged = _solutionsSystem.TryGetSolution(uid, PowerCellComponent.SolutionName, out var solution)
                               && solution.ContainsReagent("Plasma", out var plasma)
                               && plasma >= 5;

        if (component.IsRigged)
        {
            _logSystem.Add(LogType.Explosion, LogImpact.Medium, $"Power cell {ToPrettyString(uid)} has been rigged up to explode when used.");
        }
    }

    private void OnCellExamined(EntityUid uid, PowerCellComponent component, ExaminedEvent args)
    {
        if (!TryComp(uid, out BatteryComponent? battery))
            return;

        var charge = battery.CurrentCharge / battery.MaxCharge * 100;
        args.PushMarkup(Loc.GetString("power-cell-component-examine-details", ("currentCharge", $"{charge:F0}")));
    }
}
