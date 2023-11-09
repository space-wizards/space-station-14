using Content.Server.Ganimed;
using Content.Shared.Ganimed;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Ganimed.Components
{
    [RegisterComponent]
    [Access(typeof(BookTerminalSystem))]
    public sealed partial class BookTerminalComponent : Component
    {

        [DataField("workSound")]
        public SoundSpecifier WorkSound = new SoundPathSpecifier("/Audio/Machines/tray_eject.ogg");
		
		[DataField("clickSound")]
		public SoundSpecifier ClickSound = new SoundPathSpecifier("/Audio/Machines/terminal_insert_disc.ogg");
		
		[DataField("workTimeRemaining")]
        public float WorkTimeRemaining = 0.0f;
		
		[DataField("workType")]
        public string? WorkType;
		
		[DataField("stampName"), ViewVariables(VVAccess.ReadWrite)]
        public string? StampedName = "stamp-component-stamped-name-terminal";
		
		[DataField("stampColor"), ViewVariables(VVAccess.ReadWrite)]
        public string? StampedColor = "#999999";
		
		[DataField("blockWriting"), ViewVariables(VVAccess.ReadWrite)]
        public bool BlockWriting = false;
		
		[DataField("printBookEntry")]
        public SharedBookTerminalEntry? PrintBookEntry;
		
		[DataField("workTime"), ViewVariables(VVAccess.ReadWrite)]
		public float WorkTime = 8.0f;
		
		[DataField("timeMultiplier")]
		public float TimeMultiplier = 1.0f;
		
		[DataField("cartridgeUsage"), ViewVariables(VVAccess.ReadWrite)]
		public float CartridgeUsage = 1.0f;
		
		[DataField("baseCartridgeUsage")]
		public float BaseCartridgeUsage = 1.0f;
    }
}
