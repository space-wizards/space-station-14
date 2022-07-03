using Content.Shared.Singularity.Components;

namespace Content.Server.Singularity.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedContainmentFieldGeneratorComponent))]
    public sealed class ContainmentFieldGeneratorComponent : SharedContainmentFieldGeneratorComponent
    {
        [ViewVariables]
        private int _powerBuffer;

        [ViewVariables]
        public int PowerBuffer
        {
            get => _powerBuffer;
            set => _powerBuffer = Math.Clamp(value, 0, 25); //have this decrease over time if not hit by a bolt
        }

        /// <summary>
        /// What collision should power this generator?
        /// It really shouldn't be anything but an emitter bolt but it's here for fun.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("idTag")]
        public string IDTag = "EmitterBolt";

        /// <summary>
        /// How much power should this field generator receive from a collision
        /// Also acts as the minimum the field needs to start generating a connection
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("power")]
        public int Power = 6;

        /// <summary>
        /// Store a direction + field?
        /// </summary>
        public readonly Dictionary<Direction, ContainmentFieldComponent> Connections = new();

        [ViewVariables]
        public Tuple<Direction, ContainmentFieldConnection>? Connection1;

        [ViewVariables]
        public Tuple<Direction, ContainmentFieldConnection>? Connection2;

        /// <summary>
        /// Is the generator toggled on?
        /// </summary>
        [ViewVariables]
        public bool Enabled;

        /// <summary>
        /// Is this generator connected to fields?
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public bool IsConnected;

    }
}
