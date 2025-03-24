using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Gravity
{
    [RegisterComponent]
    [NetworkedComponent]
    public sealed partial class GravityComponent : Component
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
                // Does this get fired twice on a change? Once here, once in <see cref="SharedGravitySystem.OnHandleState"/>?
                // I set dirtying on the system that sets this. But if other places call this set then will need to change this too.
                var ev = new GravityChangedEvent(Owner, value);
                IoCManager.Resolve<IEntityManager>().EventBus.RaiseLocalEvent(Owner, ref ev);
                Dirty();
            }
        }

        [DataField("enabled")]
        public bool Enabled;

        /// <summary>
        /// Inherent gravity ensures GravitySystem won't change Enabled according to the gravity generators attached to this entity.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("inherent")]
        public bool Inherent;
    }
}
