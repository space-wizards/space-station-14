using Content.Server.Mech.Systems;
using Content.Shared.ActionBlocker;
using Content.Shared.FixedPoint;
using Content.Shared.Mech.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Vehicle;
using Content.Shared.Vehicle.Components;
using System.Numerics;
using System.Linq;

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

    private readonly HashSet<EntityUid> _activeMechs = new();

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
            if (!TryComp<MechComponent>(mechUid, out var mech) || !TryComp<InputMoverComponent>(mechUid, out var mover))
            {
                _activeMechs.Remove(mechUid);
                continue;
            }

            if (mech.MovementEnergyPerSecond <= 0f)
                continue;

            if (!mover.CanMove || mover.WishDir == Vector2.Zero)
                continue;

            if (!_powerCell.TryGetBatteryFromSlot(mechUid, out var battEnt, out var battery))
                continue;

            var toDrain = mech.MovementEnergyPerSecond * frameTime;

            if (battery.CurrentCharge < toDrain)
            {
                _actionBlocker.UpdateCanMove(mechUid);
                _activeMechs.Remove(mechUid);
                continue;
            }

            _mechSystem.TryChangeEnergy(mechUid, -FixedPoint2.New(toDrain), mech);
            _actionBlocker.UpdateCanMove(mechUid);
        }
    }
}
