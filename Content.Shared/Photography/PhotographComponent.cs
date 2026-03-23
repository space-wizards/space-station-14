using Robust.Shared.GameStates;

namespace Content.Shared.Photography;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class PhotographComponent : Component
{
    [DataField, AutoNetworkedField]
    public byte[]? RawData;
}
