using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Random;

namespace Content.Server.GameObjects.Components.Damage
{
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

        protected override void DestructionBehavior()
        {
            _actSystem.HandleBreakage(Owner);
        }
    }
}
