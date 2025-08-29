using Content.Server.Mech.Systems;
using Content.Shared.ActionBlocker;
using Content.Shared.FixedPoint;
using Content.Shared.Mech.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Vehicle;
using System.Numerics;

namespace Content.Server.Mech.Systems;

/// <summary>
/// Handles per-frame movement energy drain for mechs to avoid an Update override in MechSystem.
/// </summary>
public sealed class MechMovementEnergySystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly VehicleSystem _vehicle = default!;
    [Dependency] private readonly MechSystem _mechSystem = default!;
    [Dependency] private readonly PowerCell.PowerCellSystem _powerCell = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var enumerate = EntityQueryEnumerator<MechComponent, InputMoverComponent>();
        while (enumerate.MoveNext(out var mechUid, out var mech, out var mover))
        {
            if (mech.MovementEnergyPerSecond <= 0f)
                continue;

            if (!_vehicle.HasOperator(mechUid))
                continue;

            if (!mover.CanMove)
                continue;

            if (mover.WishDir == Vector2.Zero)
                continue;

            if (!_powerCell.TryGetBatteryFromSlot(mechUid, out var battEnt, out var battery))
                continue;

            if (battery.CurrentCharge <= 0f)
                continue;

            var toDrain = mech.MovementEnergyPerSecond * frameTime;
            if (toDrain <= 0f)
                continue;

            // If requested drain exceeds remaining charge, clamp battery to zero.
            if (battery.CurrentCharge < toDrain)
            {
                EntityManager.System<Power.EntitySystems.BatterySystem>().SetCharge(battEnt.Value, 0f, battery);
                _actionBlocker.UpdateCanMove(mechUid);
                continue;
            }

            _mechSystem.TryChangeEnergy(mechUid, -FixedPoint2.New(toDrain), mech);
            _actionBlocker.UpdateCanMove(mechUid);
        }
    }
}

