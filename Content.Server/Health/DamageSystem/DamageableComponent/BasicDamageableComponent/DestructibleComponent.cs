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
    ///     When attached to an <see cref="IEntity"/>, allows it to take damage and deletes it after taking enough damage.
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(IDamageableComponent))]
    public class DestructibleComponent : BasicRuinableComponent, IDestroyAct
    {
#pragma warning disable 649
        [Dependency] private readonly IEntitySystemManager _entitySystemManager;
#pragma warning restore 649

        /// <inheritdoc />
        public override string Name => "Destructible";

        /// <summary>
        ///     Entity spawned upon destruction.
        /// </summary>
        public string SpawnOnDestroy => _spawnOnDestroy;

        protected string _spawnOnDestroy;

        protected ActSystem _actSystem;

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
                _actSystem.HandleDestruction(Owner, true); //This will call IDestroyAct.OnDestroy on this component (and all other components on this entity)
                if (_destroySound != string.Empty)
                {
                    EntitySystem.Get<AudioSystem>().PlayAtCoords(_destroySound, pos);
                }
            }
        }

        void IDestroyAct.OnDestroy(DestructionEventArgs eventArgs)
        {
            if (!string.IsNullOrWhiteSpace(_spawnOnDestroy) && eventArgs.IsSpawnWreck)
            {
                Owner.EntityManager.SpawnEntity(_spawnOnDestroy, Owner.Transform.GridPosition);
            }
        }
    }
}
