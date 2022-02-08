using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Speech
{
    /// <summary>
    ///     Component required for entities to be able to speak.
    /// </summary>
    [RegisterComponent]
    public class SharedSpeechComponent : Component
    {
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
    }
}
