using System.Collections.Generic;
using Content.Shared.Audio;
using Content.Shared.GameObjects.Components.Damage;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using Logger = Robust.Shared.Log.Logger;

namespace Content.Server.GameObjects.Components.Damage
{
    /// <summary>
    ///     When attached to an <see cref="IEntity"/>, allows it to take damage and
    ///     "ruins" or "destroys" it after enough damage is taken.
    /// </summary>
    [ComponentReference(typeof(IDamageableComponent))]
    public abstract class RuinableComponent : DamageableComponent
    {
        /// <summary>
        ///     Sound played upon destruction.
        /// </summary>
        [ViewVariables]
        protected string DestroySound { get; private set; }

        /// <summary>
        /// Used instead of <see cref="DestroySound"/> if specified.
        /// </summary>
        [ViewVariables]
        protected string DestroySoundCollection { get; private set; }

        public override List<DamageState> SupportedDamageStates =>
            new() {DamageState.Alive, DamageState.Dead};

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataReadWriteFunction(
                "deadThreshold",
                100,
                t =>
                {
                    if (t == null)
                    {
                        return;
                    }

                    Thresholds[DamageState.Dead] = t.Value;
                },
                () => Thresholds.TryGetValue(DamageState.Dead, out var value) ? value : (int?) null);

            serializer.DataField(this, ruinable => ruinable.DestroySound, "destroySound", string.Empty);
            serializer.DataField(this, ruinable => ruinable.DestroySoundCollection, "destroySoundCollection", string.Empty);
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
        ///     <see cref="IDamageableComponent.CurrentState"/> to
        ///     <see cref="Shared.GameObjects.Components.Damage.DamageState.Dead"/>
        /// </summary>
        protected void PerformDestruction()
        {
            CurrentState = DamageState.Dead;

            if (!Owner.Deleted)
            {
                var pos = Owner.Transform.Coordinates;
                string sound = string.Empty;
                if (DestroySoundCollection != string.Empty)
                {
                    sound = AudioHelpers.GetRandomFileFromSoundCollection(DestroySoundCollection);

                }
                else if (DestroySound != string.Empty)
                {
                    sound = DestroySound;
                }
                if (sound != string.Empty)
                {
                    Logger.Debug("Playing destruction sound");
                    EntitySystem.Get<AudioSystem>().PlayAtCoords(sound, pos, AudioHelpers.WithVariation(0.125f));
                }
            }

            DestructionBehavior();
        }

        protected abstract void DestructionBehavior();
    }
}
