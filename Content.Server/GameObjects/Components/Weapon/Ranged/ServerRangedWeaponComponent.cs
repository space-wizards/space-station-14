using System;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.Components.Weapon.Ranged.Barrels;
using Content.Shared.Damage;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.Components.Weapons.Ranged;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Players;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Weapon.Ranged
{
    [RegisterComponent]
    public sealed class ServerRangedWeaponComponent : SharedRangedWeaponComponent, IHandSelected
    {
        private TimeSpan _lastFireTime;

        [ViewVariables(VVAccess.ReadWrite)]
        public bool ClumsyCheck { get; set; }
        [ViewVariables(VVAccess.ReadWrite)]
        public float ClumsyExplodeChance { get; set; }

        public Func<bool> WeaponCanFireHandler;
        public Func<IEntity, bool> UserCanFireHandler;
        public Action<IEntity, Vector2> FireHandler;

        public ServerRangedBarrelComponent Barrel
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
        private ServerRangedBarrelComponent _barrel;

        private FireRateSelector FireRateSelector => _barrel?.FireRateSelector ?? FireRateSelector.Safety;

        private bool WeaponCanFire()
        {
            return WeaponCanFireHandler == null || WeaponCanFireHandler();
        }

        private bool UserCanFire(IEntity user)
        {
            return (UserCanFireHandler == null || UserCanFireHandler(user)) && ActionBlockerSystem.CanAttack(user);
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(this, p => p.ClumsyCheck, "clumsyCheck", true);
            serializer.DataField(this, p => p.ClumsyExplodeChance, "clumsyExplodeChance", 0.5f);
        }

        /// <inheritdoc />
        public override void HandleNetworkMessage(ComponentMessage message, INetChannel channel, ICommonSession session = null)
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
                        if (!IoCManager.Resolve<IMapManager>().TryGetGrid(msg.TargetGrid, out var grid))
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
        private void TryFire(IEntity user, Vector2 targetPos)
        {
            if (!user.TryGetComponent(out HandsComponent hands) || hands.GetActiveHand?.Owner != Owner)
            {
                return;
            }

            if(!user.TryGetComponent(out CombatModeComponent combat) || !combat.IsInCombatMode) {
                return;
            }

            if (!UserCanFire(user) || !WeaponCanFire())
            {
                return;
            }

            var curTime = IoCManager.Resolve<IGameTiming>().CurTime;
            var span = curTime - _lastFireTime;
            if (span.TotalSeconds < 1 / _barrel.FireRate)
            {
                return;
            }

            _lastFireTime = curTime;

            if (ClumsyCheck &&
                user.HasComponent<ClumsyComponent>() &&
                IoCManager.Resolve<IRobustRandom>().Prob(ClumsyExplodeChance))
            {
                var soundSystem = EntitySystem.Get<AudioSystem>();
                soundSystem.PlayAtCoords("/Audio/Items/bikehorn.ogg",
                    Owner.Transform.GridPosition, AudioParams.Default, 5);

                soundSystem.PlayAtCoords("/Audio/Weapons/Guns/Gunshots/bang.ogg",
                    Owner.Transform.GridPosition, AudioParams.Default, 5);

                if (user.TryGetComponent(out IDamageableComponent health))
                {
                    health.ChangeDamage(DamageType.Blunt, 10, false, user);
                    health.ChangeDamage(DamageType.Heat, 5, false, user);
                }

                if (user.TryGetComponent(out StunnableComponent stun))
                {
                    stun.Paralyze(3f);
                }

                user.PopupMessage(Loc.GetString("The gun blows up in your face!"));

                Owner.Delete();
                return;
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
