using Robust.Shared.GameStates;

namespace Content.Shared.DetailExaminable;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DetailExaminableComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public string Content = string.Empty;
}
