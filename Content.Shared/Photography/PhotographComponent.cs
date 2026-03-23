using Robust.Shared.GameStates;

namespace Content.Shared.Photography;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PhotographComponent : Component
{
    [DataField, AutoNetworkedField]
    public string ImageText = "";
}
