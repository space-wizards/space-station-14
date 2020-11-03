#nullable enable
using Content.Shared.Audio;
using Content.Shared.GameObjects.Components.Damage;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using Logger = Robust.Shared.Log.Logger;

namespace Content.Server.GameObjects.Components.Damage
{
    /// <summary>
    ///     When attached to an <see cref="IEntity"/>, allows it to take damage and
    ///     "ruins" or "destroys" it after enough damage is taken.
    /// </summary>
    public abstract class RuinableComponent : Component
    {
        /// <summary>
        ///     Sound played upon destruction.
        /// </summary>
        [ViewVariables]
        protected string DestroySound { get; private set; } = string.Empty;

        /// <summary>
        /// Used instead of <see cref="DestroySound"/> if specified.
        /// </summary>
        [ViewVariables]
        protected string DestroySoundCollection { get; private set; } = string.Empty;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(this, ruinable => ruinable.DestroySound, "destroySound", string.Empty);
            serializer.DataField(this, ruinable => ruinable.DestroySoundCollection, "destroySoundCollection", string.Empty);
        }

        public override void HandleMessage(ComponentMessage message, IComponent? component)
        {
            base.HandleMessage(message, component);

            switch (message)
            {
                case DamageStateChangeMessage msg:
                {
                    if (msg.State == DamageState.Dead)
                    {
                        PerformDestruction();
                    }

                    break;
                }
            }
        }

        /// <summary>
        ///     Destroys the Owner <see cref="IEntity"/>, setting
        ///     <see cref="IDamageableComponent.CurrentState"/> to
        ///     <see cref="DamageState.Dead"/>
        /// </summary>
        protected void PerformDestruction()
        {
            if (Owner.Deleted)
            {
                return;
            }

            if (Owner.TryGetComponent(out IDamageableComponent? damageable))
            {
                damageable.CurrentState = DamageState.Dead;
            }

            var pos = Owner.Transform.Coordinates;
            var sound = string.Empty;
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
    }
}
