using System.Collections.Generic;
using Content.Server.GameObjects.Components.Stack;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
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
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        protected ActSystem ActSystem;

        /// <inheritdoc />
        public override string Name => "Destructible";

        /// <summary>
        /// Entities spawned on destruction plus the min and max amount spawned.
        /// </summary>
        public Dictionary<string, List<int>> SpawnOnDestroy { get; private set; }

        void IDestroyAct.OnDestroy(DestructionEventArgs eventArgs)
        {
            if (SpawnOnDestroy == null || !eventArgs.IsSpawnWreck) return;
            foreach (var (key, value) in SpawnOnDestroy)
            {
                int count;
                if (value == null)
                {
                    count = 1;
                }
                else if (value.Count == 1 || value[0] == value[1])
                {
                    count = value[0];
                }
                else
                {
                    count = _random.Next(value[0], value[1] + 1);
                }

                if (count == 0) continue;

                var proto = _prototypeManager.Index<EntityPrototype>(key);
                if (proto.Components.ContainsKey("Stack"))
                {
                    var spawned = Owner.EntityManager.SpawnEntity(key, Owner.Transform.Coordinates);
                    var stack = spawned.GetComponent<StackComponent>();
                    stack.Count = count;
                }
                else
                {
                    for (var i = 0; i < count; i++)
                    {
                        Owner.EntityManager.SpawnEntity(key, Owner.Transform.Coordinates);
                    }
                }
            }
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);


            serializer.DataField(this, d => d.SpawnOnDestroy, "spawnOnDestroy", null);
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
                var pos = Owner.Transform.Coordinates;
                ActSystem.HandleDestruction(Owner,
                    true); //This will call IDestroyAct.OnDestroy on this component (and all other components on this entity)
                if (!Owner.Deleted && DestroySounds.Count > 0)
                {
                    EntitySystem.Get<AudioSystem>()
                        .PlayAtCoords(DestroySounds.Count == 1 ? DestroySounds[0] : _random.Pick(DestroySounds), pos);
                }
            }
        }
    }
}
