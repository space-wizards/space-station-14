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
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;

        protected ActSystem ActSystem;

        /// <inheritdoc />
        public override string Name => "Destructible";

        /// <summary>
        ///     Entity spawned upon destruction.
        /// </summary>
        public string SpawnOnDestroy { get; set; }

        void IDestroyAct.OnDestroy(DestructionEventArgs eventArgs)
        {
            if (!string.IsNullOrWhiteSpace(SpawnOnDestroy) && eventArgs.IsSpawnWreck)
            {
                Owner.EntityManager.SpawnEntity(SpawnOnDestroy, Owner.Transform.GridPosition);
            }
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(this, d => d.SpawnOnDestroy, "spawnondestroy", string.Empty);
        }

        public override void Initialize()
        {
            base.Initialize();
            ActSystem = _entitySystemManager.GetEntitySystem<ActSystem>();
        }


        protected override void DestructionBehavior()
        {
            if (!Owner.Deleted)
            {
                var pos = Owner.Transform.GridPosition;
                ActSystem.HandleDestruction(Owner,
                    true); //This will call IDestroyAct.OnDestroy on this component (and all other components on this entity)
                if (DestroySound != string.Empty)
                {
                    EntitySystem.Get<AudioSystem>().PlayAtCoords(DestroySound, pos);
                }
            }
        }
    }
}
