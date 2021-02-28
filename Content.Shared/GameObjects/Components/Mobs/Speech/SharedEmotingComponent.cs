#nullable enable
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.GameObjects.Components.Mobs.Speech
{
    [RegisterComponent]
    public class SharedEmotingComponent : Component, IActionBlocker
    {
        [DataField("enabled")] private bool _enabled = true;
        public override string Name => "Emoting";

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

        bool IActionBlocker.CanEmote() => Enabled;
    }
}
