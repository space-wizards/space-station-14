#nullable enable
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Server.GameObjects.Components.Explosion;
using Robust.Shared.GameObjects;
using System;
using System.Threading.Tasks;
using Robust.Shared.Timers;
using Robust.Shared.ViewVariables;
using Robust.Shared.Log;

namespace Content.Server.GameObjects.Components.Explosives
{
    [RegisterComponent]
    public class SeveralExplosiveComponent : Component, IInteractUsing, IActivate {

        public override string Name => "SeveralExplosive";

        [ViewVariables]
        private int _grenadesCounter = 0;
        private ExplosiveComponent? _explosiveComponent;

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs args){
            try{
                if (_grenadesCounter >= 4){
                    return false;
                }
                ExplosiveComponent? grenade;
                bool explosive = args.Using.TryGetComponent<ExplosiveComponent>(out grenade);
                if (!explosive){
                    return false;
                }
                if (_explosiveComponent == null){
                    return false;
                }
                if (_grenadesCounter == 0){
                    _explosiveComponent.DevastationRange += grenade.DevastationRange;
                    _explosiveComponent.HeavyImpactRange += grenade.HeavyImpactRange;
                    _explosiveComponent.LightImpactRange += grenade.LightImpactRange;
                }
                else{
                    if (_explosiveComponent.DevastationRange != grenade.DevastationRange ||
                        _explosiveComponent.HeavyImpactRange != grenade.HeavyImpactRange ||
                        _explosiveComponent.LightImpactRange != grenade.LightImpactRange){
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
            bool explosive = Owner.TryGetComponent<ExplosiveComponent>(out _explosiveComponent);
            if (!explosive){
                Logger.Log(LogLevel.Warning, "SeveralExplosive component need Explosive component but no one was found");
            }
        }

        void IActivate.Activate(ActivateEventArgs eventArgs){
            int counter;
            int delay;
            for (counter = 0, delay = 1500; counter != _grenadesCounter; delay += 1500, counter++){
                Timer.Spawn(delay, () =>{
                    _explosiveComponent.Explosion(false);
                });
            }
            delay += 1500;
            Timer.Spawn(delay, () =>{
                Owner.Delete();
            });
        }
    }
}
