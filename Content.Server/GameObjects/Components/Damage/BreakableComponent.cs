using System.Collections.Generic;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Random;

namespace Content.Server.GameObjects.Components.Damage
{
    // TODO: Repair needs to set CurrentDamageState to DamageState.Alive, but it doesn't exist... should be easy enough if it's just an interface you can slap on BreakableComponent

    /// <summary>
    ///     When attached to an <see cref="IEntity"/>, allows it to take damage and sets it to a "broken state" after taking
    ///     enough damage.
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(IDamageableComponent))]
    public class BreakableComponent : RuinableComponent, IExAct
    {
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        public override string Name => "Breakable";

        private ActSystem _actSystem;
        private DamageState _currentDamageState;

        public override List<DamageState> SupportedDamageStates =>
            new List<DamageState> {DamageState.Alive, DamageState.Dead};

        public override DamageState CurrentDamageState => _currentDamageState;

        void IExAct.OnExplosion(ExplosionEventArgs eventArgs)
        {
            switch (eventArgs.Severity)
            {
                case ExplosionSeverity.Destruction:
                case ExplosionSeverity.Heavy:
                    PerformDestruction();
                    break;
                case ExplosionSeverity.Light:
                    if (_random.Prob(0.5f))
                    {
                        PerformDestruction();
                    }

                    break;
            }
        }

        public override void Initialize()
        {
            base.Initialize();
            _actSystem = _entitySystemManager.GetEntitySystem<ActSystem>();
        }

        // Might want to move this down and have a more standardized method of revival
        public void FixAllDamage()
        {
            Heal();
            _currentDamageState = DamageState.Alive;
        }

        protected override void DestructionBehavior()
        {
            _actSystem.HandleBreakage(Owner);
            if (!Owner.Deleted && DestroySound != string.Empty)
            {
                var pos = Owner.Transform.Coordinates;
                EntitySystem.Get<AudioSystem>().PlayAtCoords(DestroySound, pos);
            }
        }
    }
}
