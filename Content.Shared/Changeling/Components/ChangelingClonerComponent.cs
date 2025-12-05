using Content.Shared.Charges.Components;
using Content.Shared.Cloning;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Changeling.Components;

/// <summary>
/// Changeling transformation in item form!
/// An entity with this component works like an implanter:
/// First you use it on a humanoid to make a copy of their identity, along with all species relevant components,
/// then use it on someone else to tranform them into a clone of them.
/// Can be used in combination with <see cref="LimitedChargesComponent"/>
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ChangelingClonerComponent : Component
{
    /// <summary>
    /// A clone of the player you have copied the identity from.
    /// This is a full humanoid backup, stored on a paused map.
    /// </summary>
    /// <remarks>
    /// Since this entity is stored on a separate map it will be outside PVS range.
    /// </remarks>
    [DataField, AutoNetworkedField]
    public EntityUid? ClonedBackup;

    /// <summary>
    /// Current state of the item.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ChangelingClonerState State = ChangelingClonerState.Empty;

    /// <summary>
    /// The cloning settings to use.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<CloningSettingsPrototype> Settings = "ChangelingCloningSettings";

    /// <summary>
    /// Doafter time for drawing and injecting.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan DoAfter = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Can this item be used more than once?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Reusable = true;

    /// <summary>
    /// Whether or not to add a reset verb to purge the stored identity,
    /// allowing you to draw a new one.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool CanReset = true;

    /// <summary>
    /// Raise events when renaming the target?
    /// This will change their ID card, crew manifest entry, and so on.
    /// For admeme purposes.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool RaiseNameChangeEvents;

    /// <summary>
    /// The sound to play when taking someone's identity with the item.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? DrawSound;

    /// <summary>
    /// The sound to play when someone is transformed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? InjectSound;
}

/// <summary>
/// Current state of the item.
/// </summary>
[Serializable, NetSerializable]
public enum ChangelingClonerState : byte
{
    /// <summary>
    /// No sample taken yet.
    /// </summary>
    Empty,
    /// <summary>
    /// Filled with a DNA sample.
    /// </summary>
    Filled,
    /// <summary>
    /// Has been used (single use only).
    /// </summary>
    Spent,
}
