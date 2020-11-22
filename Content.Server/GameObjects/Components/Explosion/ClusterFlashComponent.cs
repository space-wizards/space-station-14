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

namespace Content.Server.GameObjects.Components.Explosives
{
    [RegisterComponent]
    public class ClusterFlashComponent : Component, IInteractUsing, IUse{

        public override string Name => "ClusterFlash";

        private Container? _grenadesContainer;
        private int _maxGrenadesNum;
        private bool _startFull;

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs args){
            if (_grenadesContainer == null || _grenadesContainer.ContainedEntities.Count >= _maxGrenadesNum || !args.Using.HasComponent<FlashExplosiveComponent>()){
                return false;
            }
            _grenadesContainer.Insert(args.Using);
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
            int counter;
            int delay;
            for (counter = 0, delay = 1500; counter != _grenadesContainer.ContainedEntities.Count; counter++){
                Timer.Spawn(delay, () =>{
                    try{
                        if (_grenadesContainer.ContainedEntities[0].TryGetComponent<FlashExplosiveComponent>(out var grenadeFlashComponent)){
                            grenadeFlashComponent.Explode();
                        }
                    }
                    catch{
                        Logger.Log(LogLevel.Error, "Can't create explosion in SeveralFlashExplosive");
                    }
                });
                if (_grenadesContainer.ContainedEntities[0].TryGetComponent<FlashExplosiveComponent>(out var grenadeFlashComponent)){
                    delay += Convert.ToInt32(grenadeFlashComponent.Duration * 1000);
                }
            }
            delay += 100;
            Timer.Spawn(delay, () =>{
                Owner.Delete();
            });
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
        }
    }
}
