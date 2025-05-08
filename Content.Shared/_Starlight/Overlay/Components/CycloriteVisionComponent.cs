using Content.Shared.Eye.Blinding.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Eye.Blinding.Components;

[RegisterComponent]
[NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class CycloriteVisionComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Active = true;
}
