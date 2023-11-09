using Robust.Shared.Serialization;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Ganimed.Components
{

	[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
	public sealed partial class BookTerminalVisualsComponent : Component
	{
		[DataField("doWorkAnimation"), AutoNetworkedField]
        public bool DoWorkAnimation = false;
	}

	[Serializable, NetSerializable]
	public enum BookTerminalVisualLayers : byte
	{
		Base,
		Working,
		Slotted,
		Full,
		High,
		Medium,
		Low,
		None
	}
}
