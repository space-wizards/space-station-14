using System.Collections.Generic;
using Content.Shared.GameObjects.Components.Damage;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Damage
{
    /// <summary>
    ///     When attached to an <see cref="IEntity"/>, allows it to take damage and
    ///     "ruins" or "destroys" it after enough damage is taken.
    /// </summary>
    [ComponentReference(typeof(IDamageableComponent))]
    public abstract class RuinableComponent : DamageableComponent
    {
        private DamageState _currentDamageState;

        /// <summary>
        ///     Sound played upon destruction.
        /// </summary>
        protected string DestroySound { get; private set; }

        public override List<DamageState> SupportedDamageStates =>
            new List<DamageState> {DamageState.Alive, DamageState.Dead};

        public override DamageState CurrentDamageState => _currentDamageState;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataReadWriteFunction(
                "deadThreshold",
                100,
                t => DeadThreshold = t ,
                () => DeadThreshold ?? -1);

            serializer.DataField(this, ruinable => ruinable.DestroySound, "destroySound", string.Empty);
        }

        protected override void EnterState(DamageState state)
        {
            base.EnterState(state);

            if (state == DamageState.Dead)
            {
                PerformDestruction();
            }
        }

        /// <summary>
        ///     Destroys the Owner <see cref="IEntity"/>, setting
        ///     <see cref="IDamageableComponent.CurrentDamageState"/> to
        ///     <see cref="DamageState.Dead"/>
        /// </summary>
        protected void PerformDestruction()
        {
            _currentDamageState = DamageState.Dead;

            if (!Owner.Deleted && DestroySound != string.Empty)
            {
                var pos = Owner.Transform.GridPosition;
                EntitySystem.Get<AudioSystem>().PlayAtCoords(DestroySound, pos);
            }

            DestructionBehavior();
        }

        protected abstract void DestructionBehavior();
    }
}
