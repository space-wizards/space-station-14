using Content.Shared.DisplacementMap;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Hands.Components;

/// <summary>
/// Allows this entity to have hands so that it can interact with items.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
[Access(typeof(SharedHandsSystem))]
public sealed partial class HandsComponent : Component, IComponentDelta
{
    /// <inheritdoc />
    public GameTick LastFieldUpdate { get; set; }

    /// <inheritdoc />
    public GameTick[] LastModifiedFields { get; set; }

    /// <summary>
    /// The currently active hand.
    /// </summary>
    [DataField]
    public string? ActiveHandId;

    /// <summary>
    /// Intrinsic hands to be added on map init.
    /// </summary>
    [DataField]
    public Dictionary<string, Hand> StartingHands = new();

    /// <summary>
    /// Contains all hands this entity currently has.
    /// Dictionary relating a unique hand ID corresponding to a container slot on the attached entity to a class containing information about the Hand itself.
    /// Do not set this in yaml if you want to add intrinsic hands. Use <see cref="StartingHands"/> instead.
    /// </summary>
    [DataField]
    public Dictionary<string, Hand> Hands = new();

    /// <summary>
    /// The number of hands
    /// </summary>
    [ViewVariables]
    public int Count => Hands.Count;

    /// <summary>
    /// List of hand-names. These are keys for <see cref="Hands"/>. The order of this list determines the order in which hands are iterated over.
    /// </summary>
    [DataField]
    public List<string> SortedHands = new();

    /// <summary>
    /// If true, the items in the hands won't be affected by explosions.
    /// </summary>
    [DataField]
    public bool DisableExplosionRecursion;

    /// <summary>
    /// Modifies the speed at which items are thrown.
    /// </summary>
    [DataField]
    public float BaseThrowspeed = 11f;

    /// <summary>
    /// Distance after which longer throw targets stop increasing throw impulse.
    /// </summary>
    [DataField]
    public float ThrowRange = 8f;

    /// <summary>
    /// Whether or not to add in-hand sprites for held items. Some entities (e.g., drones) don't want these.
    /// Used by the client.
    /// </summary>
    [DataField]
    public bool ShowInHands = true;

    /// <summary>
    /// Data about the current sprite layers that the hand is contributing to the owner entity. Used for sprite in-hands.
    /// Used by the client.
    /// </summary>
    public readonly Dictionary<HandLocation, HashSet<string>> RevealedLayers = new();

    /// <summary>
    /// The time at which throws will be allowed again.
    /// </summary>
    [DataField, AutoPausedField]
    public TimeSpan NextThrowTime;

    /// <summary>
    /// The minimum time inbetween throws.
    /// </summary>
    [DataField]
    public TimeSpan ThrowCooldown = TimeSpan.FromSeconds(0.5f);

    /// <summary>
    /// Fallback displacement map applied to all sprites in the hand, unless otherwise specified
    /// </summary>
    [DataField]
    public DisplacementData? HandDisplacement;

    /// <summary>
    /// If defined, applies to all sprites in the left hand, ignoring <see cref="HandDisplacement"/>
    /// </summary>
    [DataField]
    public DisplacementData? LeftHandDisplacement;

    /// <summary>
    /// If defined, applies to all sprites in the right hand, ignoring <see cref="HandDisplacement"/>
    /// </summary>
    [DataField]
    public DisplacementData? RightHandDisplacement;

    /// <summary>
    /// If false, hands cannot be stripped, and they do not show up in the stripping menu.
    /// </summary>
    [DataField]
    public bool CanBeStripped = true;
}

/// <summary>
/// Parameters for a single hand.
/// </summary>
[DataDefinition]
[Serializable, NetSerializable]
public partial record struct Hand
{
    [DataField]
    public HandLocation Location = HandLocation.Middle;

    /// <summary>
    /// The label to be displayed for this hand when it does not contain an entity
    /// </summary>
    [DataField]
    public LocId? EmptyLabel;

    /// <summary>
    /// The prototype ID of a "representative" entity prototype for what this hand could hold, used in the UI.
    /// It is not map-initted.
    /// </summary>
    [DataField]
    public EntProtoId? EmptyRepresentative;

    /// <summary>
    /// What this hand is allowed to hold
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// What this hand is not allowed to hold
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;

    public Hand()
    {

    }

    public Hand(HandLocation location, LocId? emptyLabel = null, EntProtoId? emptyRepresentative = null, EntityWhitelist? whitelist = null, EntityWhitelist? blacklist = null)
    {
        Location = location;
        EmptyLabel = emptyLabel;
        EmptyRepresentative = emptyRepresentative;
        Whitelist = whitelist;
        Blacklist = blacklist;
    }
}

// If you add more fields make sure to also add them to the RegisterFields call in SharedHandsSystem!
// This is needed for delta states.
[Serializable, NetSerializable]
public sealed class HandsComponentState(
    string? activeHandId,
    Dictionary<string, Hand> hands,
    List<string> sortedHands,
    bool showInHands,
    DisplacementData? handDisplacement,
    DisplacementData? leftHandDisplacement,
    DisplacementData? rightHandDisplacement,
    bool canBeStripped) : ComponentState
{
    public string? ActiveHandId = activeHandId;
    public readonly Dictionary<string, Hand> Hands = new(hands);
    public readonly List<string> SortedHands = new(sortedHands);
    public readonly bool ShowInHands = showInHands;
    public readonly DisplacementData? HandDisplacement = handDisplacement == null ? null : new(handDisplacement);
    public readonly DisplacementData? LeftHandDisplacement = leftHandDisplacement == null ? null : new(leftHandDisplacement);
    public readonly DisplacementData? RightHandDisplacement = rightHandDisplacement == null ? null : new(rightHandDisplacement);
    public readonly bool CanBeStripped = canBeStripped;
}

/// <summary>
/// Delta state for the active hand so that we don't have to network
/// the entire component inluding displacements each time we switch hands.
/// </summary>
[Serializable, NetSerializable]
public sealed class HandsComponentActiveHandDeltaState(string? activeHandId) : IComponentDeltaState<HandsComponentState>
{
    public string? ActiveHandId = activeHandId;

    public void ApplyToFullState(HandsComponentState fullState)
    {
        fullState.ActiveHandId = ActiveHandId;
    }

    public HandsComponentState CreateNewFullState(HandsComponentState fullState)
    {
        var newState = new HandsComponentState(
            fullState.ActiveHandId,
            fullState.Hands,
            fullState.SortedHands,
            fullState.ShowInHands,
            fullState.HandDisplacement,
            fullState.LeftHandDisplacement,
            fullState.RightHandDisplacement,
            fullState.CanBeStripped)
        {
            ActiveHandId = fullState.ActiveHandId,
        };
        return newState;
    }
}

/// <summary>
/// What side of the body this hand is on.
/// </summary>
public enum HandLocation : byte
{
    Right,
    Middle,
    Left
}
