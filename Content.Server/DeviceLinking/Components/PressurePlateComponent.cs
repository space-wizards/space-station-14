using Content.Shared.DeviceLinking;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Audio;

namespace Content.Server.DeviceLinking.Components
{
    /// <summary>
    /// This component allows the facility to register the weight of objects above it and provide signals to devices
    /// </summary>
    [RegisterComponent]
    
    public sealed partial class PressurePlateComponent : Component
    {
        [DataField("state")]
        public PressurePlateState State = PressurePlateState.Released;

        /// <summary>
        /// All uids currently located on the platform.
        /// </summary>
        [DataField, ViewVariables(VVAccess.ReadOnly)]
        public HashSet<EntityUid> Colliding = new();

        /// <summary>
        /// The required weight of an object that happens to be above the slab to activate.
        /// </summary>
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public float WeightRequired = 50f;

        [DataField("pressedSignal", customTypeSerializer: typeof(PrototypeIdSerializer<SourcePortPrototype>))]
        public string PressedSignal = "Pressed";

        [DataField("releasedSignal", customTypeSerializer: typeof(PrototypeIdSerializer<SourcePortPrototype>))]
        public string ReleasedSignal = "Released";

        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public SoundSpecifier PressedSound = new SoundPathSpecifier("/Audio/Machines/button.ogg");

        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public SoundSpecifier ReleasedSound = new SoundPathSpecifier("/Audio/Machines/button.ogg");
    }
}
