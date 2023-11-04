using Robust.Shared.Serialization;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Ganimed.Components
{

	[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
	public sealed partial class BookTerminalCartridgeComponent : Component
	{
		[DataField("fullCharge"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
		public int FullCharge = 20;
		
		[DataField("currentCharge"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
		public int CurrentCharge = 20;
	}
}
