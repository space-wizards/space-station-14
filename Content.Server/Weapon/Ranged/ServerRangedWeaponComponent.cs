using System;
using Content.Server.Atmos.EntitySystems;
using Content.Server.CombatMode;
using Content.Server.Hands.Components;
using Content.Server.Interaction.Components;
using Content.Server.Stunnable;
using Content.Server.Weapon.Ranged.Barrels.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Damage;
using Content.Shared.Hands;
using Content.Shared.Popups;
using Content.Shared.Sound;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Players;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Timing;
using Robust.Shared.ViewVariables;

namespace Content.Server.Weapon.Ranged
{
    [RegisterComponent]
    public sealed class ServerRangedWeaponComponent : SharedRangedWeaponComponent
    {
        [Dependency] private readonly IEntityManager _entMan = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        private TimeSpan _lastFireTime;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("clumsyCheck")]
        public bool ClumsyCheck { get; set; } = true;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("clumsyExplodeChance")]
        public float ClumsyExplodeChance { get; set; } = 0.5f;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("canHotspot")]
        private bool _canHotspot = true;

        [DataField("clumsyWeaponHandlingSound")]
        private SoundSpecifier _clumsyWeaponHandlingSound = new SoundPathSpecifier("/Audio/Items/bikehorn.ogg");

        [DataField("clumsyWeaponShotSound")]
        private SoundSpecifier _clumsyWeaponShotSound = new SoundPathSpecifier("/Audio/Weapons/Guns/Gunshots/bang.ogg");

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("clumsyDamage")]
        public DamageSpecifier? ClumsyDamage;

        public Func<bool>? WeaponCanFireHandler;
        public Func<EntityUid, bool>? UserCanFireHandler;
        public Action<EntityUid, Vector2>? FireHandler;

        public ServerRangedBarrelComponent? Barrel
        {
            get => _barrel;
            set
            {
                if (_barrel != null && value != null)
                {
                    Logger.Error("Tried setting Barrel on RangedWeapon that already has one");
                    throw new InvalidOperationException();
                }

                _barrel = value;
                Dirty();
            }
        }
        private ServerRangedBarrelComponent? _barrel;

        private FireRateSelector FireRateSelector => _barrel?.FireRateSelector ?? FireRateSelector.Safety;

        private bool WeaponCanFire()
        {
            return WeaponCanFireHandler == null || WeaponCanFireHandler();
        }

        private bool UserCanFire(EntityUid user)
        {
            return (UserCanFireHandler == null || UserCanFireHandler(user)) && EntitySystem.Get<ActionBlockerSystem>().CanInteract(user);
        }

        /// <inheritdoc />
        [Obsolete("Component Messages are deprecated, use Entity Events instead.")]
        public override void HandleNetworkMessage(ComponentMessage message, INetChannel channel, ICommonSession? session = null)
        {
            base.HandleNetworkMessage(message, channel, session);

            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            switch (message)
            {
                case FirePosComponentMessage msg:
                    if (session.AttachedEntity is not {Valid: true} user)
                    {
                        return;
                    }

                    if (msg.TargetGrid != GridId.Invalid)
                    {
                        // grid pos
                        if (!_mapManager.TryGetGrid(msg.TargetGrid, out var grid))
                        {
                            // Client sent us a message with an invalid grid.
                            break;
                        }

                        var targetPos = grid.LocalToWorld(msg.TargetPosition);
                        TryFire(user, targetPos);
                    }
                    else
                    {
                        // map pos
                        TryFire(user, msg.TargetPosition);
                    }

                    break;
            }
        }

        public override ComponentState GetComponentState()
        {
            return new RangedWeaponComponentState(FireRateSelector);
        }

        /// <summary>
        /// Tries to fire a round of ammo out of the weapon.
        /// </summary>
        /// <param name="user">Entity that is operating the weapon, usually the player.</param>
        /// <param name="targetPos">Target position on the map to shoot at.</param>
        private void TryFire(EntityUid user, Vector2 targetPos)
        {
            if (!_entMan.TryGetComponent(user, out HandsComponent? hands) || hands.GetActiveHandItem?.Owner != Owner)
            {
                return;
            }

            if (!_entMan.TryGetComponent(user, out CombatModeComponent? combat) || !combat.IsInCombatMode)
            {
                return;
            }

            if (!UserCanFire(user) || !WeaponCanFire())
            {
                return;
            }

            var curTime = _gameTiming.CurTime;
            var span = curTime - _lastFireTime;
            if (span.TotalSeconds < 1 / _barrel?.FireRate)
            {
                return;
            }

            _lastFireTime = curTime;

            if (ClumsyCheck && ClumsyDamage != null && ClumsyComponent.TryRollClumsy(user, ClumsyExplodeChance))
            {
                //Wound them
                EntitySystem.Get<DamageableSystem>().TryChangeDamage(user, ClumsyDamage);
                EntitySystem.Get<StunSystem>().TryParalyze(user, TimeSpan.FromSeconds(3f), true);

                // Apply salt to the wound ("Honk!")
                SoundSystem.Play(
                    Filter.Pvs(Owner), _clumsyWeaponHandlingSound.GetSound(),
                    _entMan.GetComponent<TransformComponent>(Owner).Coordinates, AudioParams.Default.WithMaxDistance(5));

                SoundSystem.Play(
                    Filter.Pvs(Owner), _clumsyWeaponShotSound.GetSound(),
                    _entMan.GetComponent<TransformComponent>(Owner).Coordinates, AudioParams.Default.WithMaxDistance(5));

                user.PopupMessage(Loc.GetString("server-ranged-weapon-component-try-fire-clumsy"));

                _entMan.DeleteEntity(Owner);
                return;
            }

            if (_canHotspot)
            {
                EntitySystem.Get<AtmosphereSystem>().HotspotExpose(_entMan.GetComponent<TransformComponent>(user).Coordinates, 700, 50);
            }
            FireHandler?.Invoke(user, targetPos);
        }
    }
}
