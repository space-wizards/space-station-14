using Robust.Shared.Serialization;

namespace Content.Shared.DeathNote;

/// <summary>
/// Entity with this component is able to see Death Note's Shinigami.
/// </summary>
public abstract partial class ShinigamiSightComponent : Component
{

}

/// <summary>
/// Contains network state for ShinigamiSightComponent.
/// </summary>
[Serializable, NetSerializable]
public sealed class ShinigamiSightComponentState : ComponentState
{
    public ShinigamiSightComponentState(ShinigamiSightComponent component)
    {

    }
}
