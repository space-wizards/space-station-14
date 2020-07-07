using System.Collections.Generic;
using Content.Server.DamageSystem;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces.GameObjects.Components.Interaction;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Random;
using Robust.Shared.Serialization;

namespace Content.Server.DamageSystem
{

    /// <summary>
    ///     When attached to an <see cref="IEntity"/>, allows it to take damage and sets it to a "broken state" after taking enough damage.
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(IDamageableComponent))]
    public class BreakableComponent : BasicRuinableComponent, IExAct {

        //TODO: Repair needs to set CurrentDamageState to DamageState.Alive, but it doesn't exist... should be easy enough if it's just an interface you can slap on BreakableComponent

#pragma warning disable 649
        [Dependency] private readonly IEntitySystemManager _entitySystemManager;
#pragma warning restore 649

        public override string Name => "Breakable";

        protected ActSystem _actSystem;

        public override void Initialize()
        {
            base.Initialize();
            _actSystem = _entitySystemManager.GetEntitySystem<ActSystem>();
        }

        public void FixAllDamage() //Might want to move this down and have a more standardized method of revival
        {
            HealAllDamage();
            CurrentDamageState = DamageState.Alive;
        }

        void IExAct.OnExplosion(ExplosionEventArgs eventArgs)
        {
            var prob = IoCManager.Resolve<IRobustRandom>();
            switch (eventArgs.Severity)
            {
                case ExplosionSeverity.Destruction:
                    PerformDestruction();
                    break;
                case ExplosionSeverity.Heavy:
                    PerformDestruction();
                    break;
                case ExplosionSeverity.Light:
                    if (prob.Prob(0.5f))
                        PerformDestruction();
                    break;
            }
        }

        protected override void DestructionBehavior()
        {
            
            _actSystem.HandleBreakage(Owner);
            if (!Owner.Deleted && _destroySound != string.Empty)
            {
                var pos = Owner.Transform.GridPosition;
                EntitySystem.Get<AudioSystem>().PlayAtCoords(_destroySound, pos);
            }
        }
    }
}

