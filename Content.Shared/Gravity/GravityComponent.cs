using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Gravity
{
    [RegisterComponent]
    [NetworkedComponent]
    public sealed class GravityComponent : Component
    {
        [DataField("gravityShakeSound")]
        public SoundSpecifier GravityShakeSound { get; set; } = new SoundPathSpecifier("/Audio/Effects/alert.ogg");

        [ViewVariables(VVAccess.ReadWrite)]
        public bool EnabledVV
        {
            get => Enabled;
            set
            {
                if (Enabled == value) return;
                Enabled = value;
                var ev = new GravityChangedEvent(Owner, value);
                IoCManager.Resolve<IEntityManager>().EventBus.RaiseLocalEvent(Owner, ref ev);
                Dirty();
            }
        }

        [DataField("enabled")]
        public bool Enabled;
    }
}
