using Content.Shared.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.GameObjects.Components.Damage
{
    /// <summary>
    ///     When attached to an <see cref="IEntity"/>, allows it to take damage and sets it to a "broken state" after taking
    ///     enough damage.
    /// </summary>
    [RegisterComponent]
    public class BreakableComponent : Component, IDestroyAct
    {
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;

        public override string Name => "Breakable";

        private ActSystem _actSystem;

        public override void Initialize()
        {
            base.Initialize();
            _actSystem = _entitySystemManager.GetEntitySystem<ActSystem>();
        }

        void IDestroyAct.OnDestroy(DestructionEventArgs eventArgs)
        {
            _actSystem.HandleBreakage(Owner);
        }
    }
}
