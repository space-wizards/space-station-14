#nullable enable
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.GameObjects.Components.Mobs.Speech
{
    /// <summary>
    ///     Component required for entities to be able to speak.
    /// </summary>
    [RegisterComponent]
    public class SharedSpeechComponent : Component, IActionBlocker
    {
        public override string Name => "Speech";

        [DataField("enabled")]
        private bool _enabled = true;

        public bool Enabled
        {
            get => _enabled;
            set
            {
                if (_enabled == value) return;
                _enabled = value;
                Dirty();
            }
        }

        bool IActionBlocker.CanSpeak() => Enabled;
    }
}
