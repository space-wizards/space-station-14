#nullable enable
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Server.GameObjects.Components.Explosion;
using Robust.Shared.GameObjects;
using System.Threading.Tasks;
using Robust.Shared.Timers;
using Robust.Shared.ViewVariables;
using Robust.Shared.Log;

namespace Content.Server.GameObjects.Components.Explosives
{
    [RegisterComponent]
    public class SeveralFlashExplosiveComponent : FlashExplosiveComponent, IInteractUsing, IUse {

        public override string Name => "SeveralFlashExplosive";

        [ViewVariables]
        private int _grenadesCounter = 0;

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs args){
            try{
                if (_grenadesCounter >= 4){
                    return false;
                }
                FlashExplosiveComponent? grenade;
                bool explosive = args.Using.TryGetComponent<FlashExplosiveComponent>(out grenade);
                if (!explosive){
                    return false;
                }
                if (grenade == null){
                    return false;
                }
                if (_grenadesCounter == 0){
                   Range = grenade.Range;
                   Duration = grenade.Duration;
                }
                else{
                    if (Range != grenade.Range || Duration != grenade.Duration){
                        return false;
                    }
                }
                _grenadesCounter++;
                args.Using.Delete();
                return true;
            }
            catch{
                return false;
            }
        }

        public override void Initialize(){
            base.Initialize();
        }

        bool IUse.UseEntity(UseEntityEventArgs eventArgs){
            int counter;
            int delay;
            for (counter = 0, delay = 1500; counter != _grenadesCounter; delay += 1500, counter++){
                Timer.Spawn(delay, () =>{
                    try{
                        Explode();
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
