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

        [DataField("workSound"), ViewVariables(VVAccess.ReadWrite)]
        public SoundSpecifier WorkSound = new SoundPathSpecifier("/Audio/Machines/high_tech_confirm.ogg");
		
		[DataField("clickSound")]
		public SoundSpecifier ClickSound = new SoundPathSpecifier("/Audio/Machines/printer.ogg");
		
		[DataField("workTimeRemaining")]
        public float WorkTimeRemaining = 0.0f;
		
		[DataField("workType")]
        public string? WorkType;
		
		[DataField("printBookEntry")]
        public SharedBookTerminalEntry? PrintBookEntry;
		
		[DataField("workTime"), ViewVariables(VVAccess.ReadWrite)]
		public float WorkTime = 8.0f;
    }
}
