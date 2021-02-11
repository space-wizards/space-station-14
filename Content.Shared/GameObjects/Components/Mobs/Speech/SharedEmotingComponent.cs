using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Mobs.Speech
{
    [RegisterComponent]
    public class SharedEmotingComponent : Component, IActionBlocker
    {
        private bool _enabled = true;
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

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(this, x => x.Enabled, "enabled", true);
        }
    }
}
