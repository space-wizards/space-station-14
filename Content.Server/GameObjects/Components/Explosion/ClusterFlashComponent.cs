#nullable enable
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Server.GameObjects.Components.Explosion;
using Robust.Shared.GameObjects;
using System.Threading.Tasks;
using Robust.Shared.Timers;
using Robust.Shared.ViewVariables;
using Robust.Shared.Log;
using Robust.Server.GameObjects.Components.Container;

namespace Content.Server.GameObjects.Components.Explosives
{
    [RegisterComponent]
    public class ClusterFlashComponent : Component, IInteractUsing, IUse{

        public override string Name => "ClusterFlash";

        protected Container? _grenadesContainer;
        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs args){
            if (_grenadesContainer == null || _grenadesContainer.ContainedEntities.Count >= 4 || !args.Using.HasComponent<FlashExplosiveComponent>()){
                return false;
            }
            _grenadesContainer.Insert(args.Using);
            return true;
        }

        public override void Initialize(){
            base.Initialize();

            _grenadesContainer = ContainerManagerComponent.Ensure<Container>("clusterFlash", Owner);
        }

        bool IUse.UseEntity(UseEntityEventArgs eventArgs){
            if (_grenadesContainer == null){
                return false;
            }
            int counter;
            int delay;
            for (counter = 0, delay = 1500; counter != _grenadesContainer.ContainedEntities.Count; delay += 1500, counter++){
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
            }
            delay += 100;
            Timer.Spawn(delay, () =>{
                try{
                    Owner.Delete();
                }
                catch{
                    Logger.Log(LogLevel.Warning, "Can't delete Entity with SeveralFlashExplosiveComponent");
                }
            });
            return true;
        }
    }
}
