using Content.Server.Botany.Components;
using Robust.Shared.Containers;
using Robust.Shared.Audio.Systems;
using Content.Shared.Throwing;
using Robust.Shared.Random;
using Content.Shared.Inventory;
using Content.Shared.Damage;
using Content.Shared.Popups;
using Content.Server.Administration.Logs;


namespace Content.Server.Botany.Systems
{
    
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
        private void OnHandPickUp(EntityUid uid, ThornyComponent component, ContainerGettingInsertedAttemptEvent args)
        {

            bool hasImmunity = false;

            var user = args.Container.Owner;
            if (_inventory.TryGetSlotEntity(user, "gloves", out var slotEntity) &&
                TryComp<ThornyImmuneComponent>(slotEntity, out var immunity))
            {
                hasImmunity = immunity.ThornImmune;
            }

            if (hasImmunity == true)
            {
                return;
            }

            args.Cancel();
            _audio.PlayPvs(component.Sound, uid);
            _transform.SetCoordinates(uid, Transform(user).Coordinates);
            _transform.AttachToGridOrMap(uid);
            _throwing.TryThrow(uid, _random.NextVector2(), strength: component.ThrowStrength);
            var tookDamage = _damageableSystem.TryChangeDamage(user, component.Damage, origin: user);

            if (tookDamage != null)
            {
                _popupSystem.PopupEntity("You burn your hand touching the nettle.", uid, user);
            }
        }
    }
}
