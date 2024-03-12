using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Containers;
using Content.Shared.Weapons.Melee.Components;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Audio;

namespace Content.Server.Weapons.Melee;

/// <summary>
/// Checks if the used entity has a gas tank equipped and removes gas from tank when melee throw occurs.
/// </summary>
public sealed class GasPoweredMeleeThrowSystem : SharedGasPoweredMeleeThrowSystem
{
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly AtmosphereSystem _atmos = default!;
    [Dependency] private readonly GasTankSystem _gasTank = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<GasPoweredThrowerComponent, AttemptMeleeThrowOnHitEvent>(OnAttemptMeleeThrowOnHit);
        SubscribeLocalEvent<GasPoweredThrowerComponent, ContainerIsInsertingAttemptEvent>(OnContainerInserting);
    }

    private void OnContainerInserting(EntityUid uid, GasPoweredThrowerComponent component, ContainerIsInsertingAttemptEvent args)
    {
        if (args.Container.ID != component.TankSlotId)
            return;

        if (!TryComp<GasTankComponent>(args.EntityUid, out var gas))
            return;

        if (gas.Air.TotalMoles >= component.GasUsage && component.GasUsage > 0f)
            return;

        args.Cancel();
    }
    private void OnAttemptMeleeThrowOnHit(Entity<GasPoweredThrowerComponent> ent, ref AttemptMeleeThrowOnHitEvent args)
    {
        var (uid, comp) = ent;

        args.Cancelled = false;
        args.Handled = true;

        var gas = GetGas(ent);
        if (gas == null && comp.GasUsage > 0f)
        {
            args.Cancelled = true;
            return;
        }

        Audio.PlayPvs(comp.HitSound, uid, AudioParams.Default.WithVariation(0.025f).WithVolume(8f));

        if (gas == null)
            return;

        var environment = _atmos.GetContainingMixture(ent, false, true);
        var removed = _gasTank.RemoveAir(gas.Value, comp.GasUsage);
        if (environment != null && removed != null)
        {
            _atmos.Merge(environment, removed);
        }

        if (gas.Value.Comp.Air.TotalMoles >= comp.GasUsage)
            return;

        _itemSlots.TryEject(uid, comp.TankSlotId, uid, out _);
    }

    private Entity<GasTankComponent>? GetGas(EntityUid uid)
    {
        if (!TryComp<GasPoweredThrowerComponent>(uid, out var gaspowered))
            return null;

        if (!Container.TryGetContainer(uid, gaspowered.TankSlotId, out var container) ||
            container is not ContainerSlot slot || slot.ContainedEntity is not { } contained)
            return null;

        return TryComp<GasTankComponent>(contained, out var gasTank) ? (contained, gasTank) : null;
    }
}
