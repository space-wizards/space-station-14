using Content.Shared.Terminator.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Terminator.Components;

/// <summary>
/// Main terminator component
/// </summary>
[RegisterComponent, Access(typeof(SharedTerminatorSystem))]
public sealed partial class TerminatorComponent : Component
{
    /// <summary>
    /// Used to force the terminate objective's target.
    /// If null it will be a random person.
    /// </summary>
    [DataField("target")]
    public EntityUid? Target;

    /// <summary>
    /// List of objectives to give the terminator on spawn.
    /// </summary>
    [DataField("objectives", required: true, customTypeSerializer: typeof(PrototypeIdListSerializer<EntityPrototype>))]
    public List<string> Objectives;
}
