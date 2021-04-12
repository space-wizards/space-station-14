using System;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.Components.Weapon.Ranged.Barrels;
using Content.Shared.Damage;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.Components.Weapons.Ranged;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
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
using Content.Server.Atmos;

namespace Content.Server.GameObjects.Components.Weapon.Ranged
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
            return (UserCanFireHandler == null || UserCanFireHandler(user)) && ActionBlockerSystem.CanAttack(user);
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

            if(!user.TryGetComponent(out CombatModeComponent? combat) || !combat.IsInCombatMode) {
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

            if (ClumsyCheck && ClumsyComponent.TryRollClumsy(user, ClumsyExplodeChance))
            {
                SoundSystem.Play(Filter.Pvs(Owner), "/Audio/Items/bikehorn.ogg",
                    Owner.Transform.Coordinates, AudioParams.Default.WithMaxDistance(5));

                SoundSystem.Play(Filter.Pvs(Owner), "/Audio/Weapons/Guns/Gunshots/bang.ogg",
                    Owner.Transform.Coordinates, AudioParams.Default.WithMaxDistance(5));

                if (user.TryGetComponent(out IDamageableComponent? health))
                {
                    health.ChangeDamage(DamageType.Blunt, 10, false, user);
                    health.ChangeDamage(DamageType.Heat, 5, false, user);
                }

                if (user.TryGetComponent(out StunnableComponent? stun))
                {
                    stun.Paralyze(3f);
                }

                user.PopupMessage(Loc.GetString("The gun blows up in your face!"));

                Owner.Delete();
                return;
            }

            if (_canHotspot && user.Transform.Coordinates.TryGetTileAtmosphere(out var tile))
            {
                tile.HotspotExpose(700, 50);
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
