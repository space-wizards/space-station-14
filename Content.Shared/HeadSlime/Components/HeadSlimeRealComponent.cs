using Robust.Shared.GameStates;

namespace Content.Shared.HeadSlimeReal;

/// <summary>
/// Similar to <see cref="Interaction.Components.UnremoveableComponent"/>
/// except entities with this component can be removed in specific case: <see cref="CanTransfer"/>
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
[Access(typeof(SharedHeadSlimeRealSystem))]
public sealed partial class HeadSlimeRealComponent : Component
{
    [DataField("canTransfer"), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public bool CanTransfer = true;
}
