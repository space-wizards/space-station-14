using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Wieldable.Components;

/// <summary>
/// Used for objects that can be wielded in two or more hands,
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedWieldableSystem)), AutoGenerateComponentState]
public sealed partial class WieldableComponent : Component
{
    /// <summary>
    /// Sound to play when wielding.
    /// </summary>
    [DataField]
    public SoundSpecifier? WieldSound = new SoundPathSpecifier("/Audio/Effects/thudswoosh.ogg");

    /// <summary>
    /// Sound to play when unwielding.
    /// </summary>
    [DataField]
    public SoundSpecifier? UnwieldSound;

    /// <summary>
    /// Number of free hands required (excluding the item itself) required
    /// to wield it.
    /// </summary>
    [DataField]
    public int FreeHandsRequired = 1;

    /// <summary>
    /// Is the item currently being wielded?
    /// </summary>
    [AutoNetworkedField]
    public bool Wielded = false;

    /// <summary>
    /// Whether using the item inhand while wielding causes the item to unwield.
    /// Unwielding can conflict with other inhand actions.
    /// </summary>
    [DataField]
    public bool UnwieldOnUse = true;

    /// <summary>
    /// Whether switching hands will cause the item to unwield.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool UnwieldOnHandDeselected = true;

    /// <summary>
    /// Should use delay trigger after the wield/unwield?
    /// </summary>
    [DataField]
    public bool UseDelayOnWield = true;

    /// <summary>
    /// If true, the wielding can only be done if done through an alternate wielding action.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool DisallowManualWielding = false;

    /// <summary>
    /// If true, a pop-up will be displayed when wielding/unwielding the entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool DisplayPopup = true;

    /// <summary>
    /// Prefix for the inhand sprite when wielding.
    /// </summary>
    [DataField]
    public string? WieldedInhandPrefix = "wielded";

    /// <summary>
    /// Old prefix for the inhand sprite, to return to when unwielding.
    /// </summary>
    public string? OldInhandPrefix = null;
}

[Serializable, NetSerializable]
public enum WieldableVisuals : byte
{
    Wielded
}
