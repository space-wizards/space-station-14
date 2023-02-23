using Robust.Shared.GameStates;

namespace Content.Shared.HotPotato;

/// <summary>
/// Similar to <see cref="Content.Shared.Interaction.Components.UnremoveableComponent"/>
/// except entities with this component can be removed in specific case: <see cref="CanTransfer"/>
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed class HotPotatoComponent : Component
{
    /// <summary>
    /// If set to true entity can be removed by hitting entities if they have hands
    /// </summary>
    [DataField("canTransfer"), ViewVariables(VVAccess.ReadWrite)]
    public bool CanTransfer = true;
}
