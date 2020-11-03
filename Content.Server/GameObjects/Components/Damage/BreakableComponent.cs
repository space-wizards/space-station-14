#nullable enable
using Content.Shared.Audio;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Damage
{
    /// <summary>
    ///     When attached to an <see cref="IEntity"/>, allows it to take damage and sets it to a "broken state" after taking
    ///     enough damage.
    /// </summary>
    [RegisterComponent]
    public class BreakableComponent : Component
    {
        public override string Name => "Breakable";

        private ActSystem _actSystem = default!;

        /// <summary>
        ///     Sound played upon destruction.
        /// </summary>
        [ViewVariables]
        private string DestroySound { get; set; } = string.Empty;

        /// <summary>
        /// Used instead of <see cref="DestroySound"/> if specified.
        /// </summary>
        [ViewVariables]
        private string DestroySoundCollection { get; set; } = string.Empty;

        public override void Initialize()
        {
            base.Initialize();
            _actSystem = EntitySystem.Get<ActSystem>();
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
                        Break();
                    }

                    break;
                }
            }
        }

        private void Break()
        {
            if (Owner.Deleted)
            {
                return;
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

            _actSystem.HandleBreakage(Owner);
        }
    }
}
