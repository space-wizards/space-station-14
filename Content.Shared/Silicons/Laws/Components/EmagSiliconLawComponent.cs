using Content.Shared.Roles;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Silicons.Laws.Components;

/// <summary>
/// This is used for an entity that grants a special "obey" law when emagge.d
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedSiliconLawSystem))]
public sealed class EmagSiliconLawComponent : Component
{
    /// <summary>
    /// The name of the person who emagged this law provider.
    /// </summary>
    [DataField("ownerName"), ViewVariables(VVAccess.ReadWrite)]
    public string? OwnerName;

    /// <summary>
    /// Does the panel need to be open to EMAG this law provider.
    /// </summary>
    [DataField("requireOpenPanel"), ViewVariables(VVAccess.ReadWrite)]
    public bool RequireOpenPanel = true;

    /// <summary>
    /// A role given to entities with this component when they are emagged.
    /// Mostly just for admin purposes.
    /// </summary>
    [DataField("antagonistRole", customTypeSerializer: typeof(PrototypeIdSerializer<AntagPrototype>))]
    public string? AntagonistRole = "SubvertedSilicon";
}
