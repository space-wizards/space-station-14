#nullable enable
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Server.GameObjects.Components.Explosion;
using Robust.Shared.GameObjects;
using System.Threading.Tasks;
using Robust.Shared.Timers;
using Robust.Shared.ViewVariables;
using Robust.Shared.Log;
using Robust.Server.GameObjects.Components.Container;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;
using System;
using Robust.Server.GameObjects;
using Content.Shared.GameObjects.Components.Explosion;
using Content.Server.Throw;
using Robust.Shared.Map;

namespace Content.Server.GameObjects.Components.Explosives
{
    [RegisterComponent]
    public class ClusterFlashComponent : SharedClusterFlashComponent, IInteractUsing, IUse{
        private Container? _grenadesContainer;
        private int _maxGrenadesNum;
        private bool _startFull;

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs args){
            if (_grenadesContainer == null || _grenadesContainer.ContainedEntities.Count >= _maxGrenadesNum || !args.Using.HasComponent<FlashExplosiveComponent>()){
                return false;
            }
            _grenadesContainer.Insert(args.Using);
            UpdateAppearance();
            return true;
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _startFull, "startFull", false);
            serializer.DataField(ref _maxGrenadesNum, "maxGrenadesCount", 4);
        }
        public override void Initialize(){
            base.Initialize();

            _grenadesContainer = ContainerManagerComponent.Ensure<Container>("clusterFlash", Owner);

            if (_startFull){
                FillContainer();
            }
        }

        bool IUse.UseEntity(UseEntityEventArgs eventArgs){
            if (_grenadesContainer == null){
                return false;
            }
            var delay = 3500;
            while (_grenadesContainer.ContainedEntities.Count > 0){
                IEntity grenade = _grenadesContainer.ContainedEntities[0];
                if (!_grenadesContainer.Remove(grenade)){
                    continue;
                }
                Random rnd = new Random();
                float x = rnd.Next(3);
                float y = rnd.Next(3);
                if (rnd.Next(1) == 1){
                    x *= -1;
                }
                if (rnd.Next(1) == 1){
                    y *= -1;
                }
                var target = new EntityCoordinates(Owner.Uid, x, y);
                var player = new EntityCoordinates(Owner.Uid, 0, 0);
                grenade.Throw(2, target, player, throwSourceEnt: Owner);
                delay += rnd.Next(400);
                UpdateAppearance();
            }
            return true;
        }

        private void FillContainer(){
            if (_grenadesContainer == null){
                return;
            }
            for (int x = 0; x != _maxGrenadesNum; x++){
                IEntity grenade = Owner.EntityManager.SpawnEntity("GrenadeFlashBang", Owner.Transform.Coordinates);
                _grenadesContainer.Insert(grenade);
            }
            UpdateAppearance();
        }

        private void UpdateAppearance(){
            if (_grenadesContainer == null){
                return;
            }
            if (Owner.TryGetComponent<AppearanceComponent>(out AppearanceComponent? appearance))
            {
                appearance.SetData(ClusterFlashVisuals.GrenadesCounter, _grenadesContainer.ContainedEntities.Count);
            }
        }
    }
}
