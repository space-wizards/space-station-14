using System;
using System.Collections.Generic;
using Content.Server.Actions.Actions;
using Content.Shared.Weapons.Guns;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.Weapon.Guns
{
    internal sealed class GunSystem : SharedGunSystem
    {
        private List<(ShootMessage Message, IEntity User)> _queuedShoot = new();

        private const double ShootBuffer = 0.1;

        private Dictionary<EntityUid, int> _fireCounter = new();

        public override void Initialize()
        {
            base.Initialize();
            SubscribeNetworkEvent<ShootMessage>(ProcessShootInput);
        }

        private void ProcessShootInput(ShootMessage msg, EntitySessionEventArgs args)
        {
            var user = args.SenderSession.AttachedEntity;
            if (user == null) return;

            if (msg.Shots is <= 0 or > 100 ||
                msg.Coordinates.MapId != user.Transform.MapID ||
                msg.Coordinates.MapId == MapId.Nullspace)
            {
                return;
            }

            if (!EntityManager.TryGetEntity(msg.GunUid, out var gunEntity) ||
                !gunEntity.TryGetComponent(out SharedGunComponent? gunComponent) ||
                !gunEntity.TryGetContainerMan(out var containerManager) ||
                containerManager.Owner != user)
            {
                return;
            }

            var currentTime = GameTiming.CurTime;

            if ((currentTime - msg.Time).TotalSeconds > ShootBuffer)
            {
                msg.Time = currentTime - TimeSpan.FromSeconds(ShootBuffer);
            }

            if (!gunComponent.Firing)
            {
                DebugTools.Assert(!_fireCounter.ContainsKey(gunEntity.Uid));
                gunComponent.Firing = true;
                gunComponent.NextFire = msg.Time;
            }

            _queuedShoot.Add((msg, user));
            _fireCounter.TryGetValue(gunEntity.Uid, out var counter);
            _fireCounter[gunEntity.Uid] = counter + 1;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            if (_queuedShoot.Count == 0) return;

            var currentTime = GameTiming.CurTime;

            for (var i = _queuedShoot.Count - 1; i >= 0; i--)
            {
                var (msg, user) = _queuedShoot[i];

                if (msg.Time > currentTime)
                {
                    break;
                }

                if (HandleShoot(currentTime, user, msg))
                {
                    _queuedShoot.RemoveAt(i);
                    StopFire(msg.GunUid);
                }
            }
        }

        private void StopFire(EntityUid uid)
        {
            if (!_fireCounter.TryGetValue(uid, out var counter)) return;
            counter -= 1;

            if (counter <= 0)
            {
                _fireCounter.Remove(uid);
            }
            else
            {
                _fireCounter[uid] = counter;
            }
        }

        private bool HandleShoot(TimeSpan currentTime, IEntity user, ShootMessage message)
        {
            if (!EntityManager.TryGetEntity(message.GunUid, out var gunEntity))
            {
                _fireCounter.Remove(message.GunUid);
                return true;
            }

            if (!gunEntity.TryGetComponent(out SharedGunComponent? gunComponent))
            {
                return true;
            }

            // TODO: Validate
            if (!TryFire(user, gunComponent, message.Coordinates, out var shots, currentTime))
            {
                return true;
            }

            message.Shots -= shots;
            //DebugTools.Assert(message.Shots >= 0);

            if (gunComponent.SoundGunshot != null)
            {
                for (var i = 0; i < shots; i++)
                {
                    SoundSystem.Play(Filter.Pvs(gunEntity), gunComponent.SoundGunshot);
                }
            }

            if (message.Shots > 0)
            {
                return false;
            }

            return true;
        }
    }
}
