using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.StepTrigger.Components;

/// <summary>
/// Grants the attached entity immunity to step triggers.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class StepTriggerImmuneComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public EntityWhitelist? Whitelist;
}
