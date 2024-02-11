using Content.Shared.Medical.Consciousness.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Medical.Consciousness.Components;



[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(ConsciousnessSystem))]
public sealed partial class ConsciousComponent : Component
{

}
