using Robust.Shared.Serialization;

namespace Content.Shared.Medical.Respiration.Components;

[RegisterComponent]
public sealed class RespirationComponent : Component
{

}

[Serializable, NetSerializable]
public sealed class RespirationComponentState : ComponentState
{

}
