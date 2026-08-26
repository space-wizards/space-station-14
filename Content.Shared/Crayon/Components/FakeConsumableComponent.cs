using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Crayon;

/// <summary>
/// Holds information about a fake consumable, a type of food that is extremely similar to normal food but that doesn't contain
/// reagents and doesn't satiate the eater.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedFakeConsumableSystem))]
public sealed partial class FakeConsumableComponent : Component
{
    /// <summary>
    /// The identifier of this fake consumable's container.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string ContainerId = "fake_consumable_slot";

    /// <summary>
    /// How much time it takes for the fake consumable to decay and spill out its contained item.
    /// </summary>
    [DataField, AutoNetworkedField, AutoPausedField]
    public TimeSpan LifeSpan = TimeSpan.FromMinutes(1f);

    /// <summary>
    /// The sound that plays when the consumable vanishes.
    /// </summary>
    [DataField("vanishSound"), AutoNetworkedField]
    public SoundSpecifier OnVanishSound;

    /// <summary>
    /// How much time should inserting an item take.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan InsertDelay = TimeSpan.FromSeconds(7f);

    /// <summary>
    /// A blacklist of entities that cannot be inserted in this fake consumable, if null everything can be inserted.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityWhitelist? Blacklist;
}
