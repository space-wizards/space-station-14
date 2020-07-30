using Content.Server.GameObjects.EntitySystems;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Content.Server.DamageSystem
{

    /// <summary>
    ///     When attached to an <see cref="IEntity"/>, allows it to take damage and "ruins" or "destroys"
    ///     it after enough damage is taken.
    /// </summary>
    [ComponentReference(typeof(IDamageableComponent))]
    public abstract class BasicRuinableComponent : BasicDamageableComponent
    {
        public override string Name => "BasicRuinable";

        /// <summary>
        ///     How much HP this component can sustain before triggering <see cref="PerformDestruction"/>.
        /// </summary>
        public int MaxHP => _maxHP;

        /// <summary>
        ///     Sound played upon destruction.
        /// </summary>
        public string DestroySound => _destroySound;

        public override List<DamageState> SupportedDamageStates => new List<DamageState> { DamageState.Alive, DamageState.Dead };

        protected int _maxHP;

        protected string _destroySound;

        public override void Initialize()
        {
            base.Initialize();
            HealthChangedEvent += OnHealthChanged;
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _maxHP, "maxHP", 100);
            serializer.DataField(ref _destroySound, "destroySound", string.Empty);
        }

        public void OnHealthChanged(HealthChangedEventArgs e)
        {
            if (CurrentDamageState != DamageState.Dead && TotalDamage >= MaxHP)
            {
                PerformDestruction();
            }
        }

        /// <summary>
        ///     Destroys the Owner <see cref="IEntity"/>, setting <see cref="IDamageableComponent.CurrentDamageState"/> to <see cref="DamageState.Dead"/>
        /// </summary>
        protected void PerformDestruction()
        {
            CurrentDamageState = DamageState.Dead;
            if (!Owner.Deleted && _destroySound != string.Empty)
            {
                var pos = Owner.Transform.GridPosition;
                EntitySystem.Get<AudioSystem>().PlayAtCoords(_destroySound, pos);
            }
            DestructionBehavior();
        }

        protected abstract void DestructionBehavior();

    }
}
