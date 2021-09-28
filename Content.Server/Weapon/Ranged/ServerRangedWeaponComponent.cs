using System;
using Content.Server.Atmos.EntitySystems;
using Content.Server.CombatMode;
using Content.Server.Hands.Components;
using Content.Server.Interaction.Components;
using Content.Server.Stunnable.Components;
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
    public sealed class ServerRangedWeaponComponent : SharedRangedWeaponComponent, IHandSelected
    {
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
        public Func<IEntity, bool>? UserCanFireHandler;
        public Action<IEntity, Vector2>? FireHandler;

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

        private bool UserCanFire(IEntity user)
        {
            return (UserCanFireHandler == null || UserCanFireHandler(user)) && EntitySystem.Get<ActionBlockerSystem>().CanInteract(user);
        }

        /// <inheritdoc />
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
                    var user = session.AttachedEntity;
                    if (user == null)
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

        public override ComponentState GetComponentState(ICommonSession player)
        {
            return new RangedWeaponComponentState(FireRateSelector);
        }

        /// <summary>
        /// Tries to fire a round of ammo out of the weapon.
        /// </summary>
        /// <param name="user">Entity that is operating the weapon, usually the player.</param>
        /// <param name="targetPos">Target position on the map to shoot at.</param>
        private void TryFire(IEntity user, Vector2 targetPos)
        {
            if (!user.TryGetComponent(out HandsComponent? hands) || hands.GetActiveHand?.Owner != Owner)
            {
                return;
            }

            if (!user.TryGetComponent(out CombatModeComponent? combat) || !combat.IsInCombatMode)
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
                EntitySystem.Get<DamageableSystem>().TryChangeDamage(user.Uid, ClumsyDamage);

                // Knock them down
                if (user.TryGetComponent(out StunnableComponent? stun))
                {
                    stun.Paralyze(3f);
                }

                // Apply salt to the wound ("Honk!")
                SoundSystem.Play(
                    Filter.Pvs(Owner), _clumsyWeaponHandlingSound.GetSound(),
                    Owner.Transform.Coordinates, AudioParams.Default.WithMaxDistance(5));

                SoundSystem.Play(
                    Filter.Pvs(Owner), _clumsyWeaponShotSound.GetSound(),
                    Owner.Transform.Coordinates, AudioParams.Default.WithMaxDistance(5));

                user.PopupMessage(Loc.GetString("server-ranged-weapon-component-try-fire-clumsy"));

                Owner.Delete();
                return;
            }

            if (_canHotspot)
            {
                EntitySystem.Get<AtmosphereSystem>().HotspotExpose(user.Transform.Coordinates, 700, 50);
            }
            FireHandler?.Invoke(user, targetPos);
        }

        // Probably a better way to do this.
        void IHandSelected.HandSelected(HandSelectedEventArgs eventArgs)
        {
            Dirty();
        }
    }
}
