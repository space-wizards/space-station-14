using Robust.Shared.Serialization;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Ganimed.Components
{

	[RegisterComponent, NetworkedComponent]
	public sealed partial class BookTerminalVisualsComponent : Component
	{
	}

	[Serializable, NetSerializable]
	public enum BookTerminalVisualLayers : byte
	{
		Base,
		Slotted
	}
}
