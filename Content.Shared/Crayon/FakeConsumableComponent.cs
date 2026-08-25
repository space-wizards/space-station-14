using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Crayon;

/// <summary>
/// Holds information about a fake consumable, a type of food that is extremely similar to normal food but that doesn't contain
/// reagents and doesn't satiate the eater.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class FakeConsumableComponent : Component
{
    /// <summary>
    /// The identifier of this fake consumable's container.
    /// </summary>
    [DataField]
    public string ContainerId = "fake_consumable_slot";

    /// <summary>
    /// Whether when an entity ingests the fake consumable, it should also consume all the contents of the entity inside.
    /// </summary>
    [DataField]
    public bool IngestContained = true;

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
