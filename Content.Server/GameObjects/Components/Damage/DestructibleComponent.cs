using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Damage
{
    /// <summary>
    ///     When attached to an <see cref="IEntity"/>, allows it to take damage and deletes it after taking enough damage.
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(IDamageableComponent))]
    public class DestructibleComponent : RuinableComponent, IDestroyAct
    {
#pragma warning disable 649
        [Dependency] private readonly IEntitySystemManager _entitySystemManager;
#pragma warning restore 649

        protected ActSystem _actSystem;

        protected string _spawnOnDestroy;

        /// <inheritdoc />
        public override string Name => "Destructible";

        /// <summary>
        ///     Entity spawned upon destruction.
        /// </summary>
        public string SpawnOnDestroy => _spawnOnDestroy;

        void IDestroyAct.OnDestroy(DestructionEventArgs eventArgs)
        {
            if (!string.IsNullOrWhiteSpace(_spawnOnDestroy) && eventArgs.IsSpawnWreck)
            {
                Owner.EntityManager.SpawnEntity(_spawnOnDestroy, Owner.Transform.GridPosition);
            }
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _spawnOnDestroy, "spawnondestroy", string.Empty);
        }

        public override void Initialize()
        {
            base.Initialize();
            _actSystem = _entitySystemManager.GetEntitySystem<ActSystem>();
        }


        protected override void DestructionBehavior()
        {
            if (!Owner.Deleted)
            {
                var pos = Owner.Transform.GridPosition;
                _actSystem.HandleDestruction(Owner,
                    true); //This will call IDestroyAct.OnDestroy on this component (and all other components on this entity)
                if (DestroySound != string.Empty)
                {
                    EntitySystem.Get<AudioSystem>().PlayAtCoords(DestroySound, pos);
                }
            }
        }
    }
}
