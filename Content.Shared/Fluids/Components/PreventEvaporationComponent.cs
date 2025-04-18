using Robust.Shared.GameStates;

namespace Content.Shared.Fluids.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PreventEvaporationComponent : Component
{
    [AutoNetworkedField]
    public bool Active;
}
