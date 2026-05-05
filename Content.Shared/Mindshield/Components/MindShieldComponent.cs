using Content.Shared.Revolutionary;
using Robust.Shared.GameStates;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;

namespace Content.Shared.Mindshield.Components;

/// <summary>
/// This component, on a clothing item, on an implant or on an entity, prevents "mind control". This means that you won't be convertable to the revolution, for instance.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedRevolutionarySystem))]
public sealed partial class MindShieldComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<SecurityIconPrototype> MindShieldStatusIcon = "MindShieldIcon";

    /// <summary>
    /// This mindshield will only overwrite the mindshield visual of mindshields with lower priority
    /// </summary>
    [DataField]
    public int VisualPriority = 100;
}
