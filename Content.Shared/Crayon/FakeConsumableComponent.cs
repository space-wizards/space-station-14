using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Crayon;

/// <summary>
/// Holds information about a fake consumable, a type of food that is extremely similar to normal food but that doesn't contain
/// reagents and doesn't satiate the eater.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class FakeConsumableComponent : Component
{
    /// <summary>
    /// If not null, the entity that this fake consumable component is holding.
    /// </summary>
    [DataField]
    public EntProtoId? Contained;

    /// <summary>
    /// The sound that plays when the consumable vanishes.
    /// </summary>
    [DataField("vanishSound")]
    public SoundSpecifier OnVanishSound;

    /// <summary>
    /// A blacklist of entities that cannot be inserted in this fake consumable, if null everything can be inserted.
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;
}
