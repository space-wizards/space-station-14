using Robust.Shared.Serialization;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Ganimed.Components
{

	[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
	public sealed partial class BookTerminalCartridgeComponent : Component
	{
		[DataField("fullCharge"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
		public float FullCharge = 20.0f;
		
		[DataField("currentCharge"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
		public float CurrentCharge = 20.0f;
	}
}
