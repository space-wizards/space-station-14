using Content.Server.Botany.Components;
using Robust.Shared.Containers;
using Robust.Shared.Audio.Systems;
using Content.Shared.Throwing;
using Robust.Shared.Random;
using Content.Shared.Inventory;
using Content.Shared.Damage;
using Content.Shared.Popups;
using Content.Server.Administration.Logs;

namespace Content.Server.Botany.Systems;
public sealed class ThornySystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ThornyComponent, ContainerGettingInsertedAttemptEvent>(OnHandPickUp);
    }
    private void OnHandPickUp(Entity<ThornyComponent> entity, ref ContainerGettingInsertedAttemptEvent args)
    {
        var user = args.Container.Owner;

        // Verify that the entity has a gloves container slot. This returns is false, since the entity is likely a bag.
        if (_inventory.TryGetSlotContainer(user, "gloves", out var containerSlot, out var slotDefinition))
        {
            if (TryComp<ThornyImmuneComponent>(containerSlot.ContainedEntity, out var immunity))
            {
                return;
            }
        }
        else
        {
            return;
        }

        args.Cancel();
        _audio.PlayPvs(entity.Comp.Sound, entity);
        _transform.SetCoordinates(entity, Transform(user).Coordinates);
        _transform.AttachToGridOrMap(entity);
        _throwing.TryThrow(entity, _random.NextVector2(), recoil: false);
        var tookDamage = _damageableSystem.TryChangeDamage(user, entity.Comp.Damage, origin: user);

        if (tookDamage != null)
        {
            _popupSystem.PopupEntity("You burn your hand touching the nettle.", entity, user);
        }
    }
}
