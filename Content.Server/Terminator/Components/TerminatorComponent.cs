using Content.Server.Terminator.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server.Terminator.Components;

/// <summary>
/// Main terminator component, handles the target, if any, and objectives.
/// </summary>
[RegisterComponent, Access(typeof(TerminatorSystem))]
public sealed partial class TerminatorComponent : Component
{
    /// <summary>
    /// Used to force the terminate objective's target.
    /// If null it will be a random person.
    /// </summary>
    [DataField("target")]
    public EntityUid? Target;
}
