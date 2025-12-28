using System.Linq;
using System.Numerics;
using Content.Shared.ActionBlocker;
using Content.Shared.FixedPoint;
using Content.Shared.Mech.Components;
using Content.Shared.Movement.Components;
using Content.Shared.PowerCell;
using Content.Shared.Power.EntitySystems;

namespace Content.Shared.Mech.Systems;

/// <summary>
/// Handles per-frame movement energy drain for mechs to avoid.
/// </summary>
public sealed class MechMovementEnergySystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly SharedBatterySystem _battery = default!;
    [Dependency] private readonly SharedMechSystem _mech = default!;

    private readonly HashSet<EntityUid> _activeMechs = [];

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MechComponent, MechMovementDrainToggleEvent>(OnDrainToggle);
    }

    private void OnDrainToggle(EntityUid uid, MechComponent component, ref MechMovementDrainToggleEvent args)
    {
        if (args.Enabled)
            _activeMechs.Add(uid);
        else
            _activeMechs.Remove(uid);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_activeMechs.Count == 0)
            return;

        foreach (var mechUid in _activeMechs.ToArray())
        {
            if (!TryComp<MechComponent>(mechUid, out var mechComp) || !TryComp<InputMoverComponent>(mechUid, out var mover))
            {
                _activeMechs.Remove(mechUid);
                continue;
            }

            if (mechComp.MovementEnergyPerSecond <= 0f)
                continue;

            if (!mover.CanMove || mover.WishDir == Vector2.Zero)
                continue;

            if (!_powerCell.TryGetBatteryFromSlot(mechUid, out var mechBattery))
                continue;

            var charge = _battery.GetCharge(mechBattery.Value.AsNullable());
            var toDrain = mechComp.MovementEnergyPerSecond * frameTime;
            if (charge < toDrain)
            {
                _actionBlocker.UpdateCanMove(mechUid);
                _activeMechs.Remove(mechUid);
                continue;
            }

            _mech.TryChangeEnergy(mechUid, -FixedPoint2.New(toDrain));
            _actionBlocker.UpdateCanMove(mechUid);
        }
    }
}
