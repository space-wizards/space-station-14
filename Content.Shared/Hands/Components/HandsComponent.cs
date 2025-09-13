using Content.Shared.DisplacementMap;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Hands.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
[Access(typeof(SharedHandsSystem))]
public sealed partial class HandsComponent : Component
{
    /// <summary>
    ///     The currently active hand.
    /// </summary>
    [DataField]
    public string? ActiveHandId;

    /// <summary>
    /// Dictionary relating a unique hand ID corresponding to a container slot on the attached entity to a class containing information about the Hand itself.
    /// </summary>
    [DataField]
    public Dictionary<string, Hand> Hands = new();

    /// <summary>
    /// The number of hands
    /// </summary>
    [ViewVariables]
    public int Count => Hands.Count;

    /// <summary>
    ///     List of hand-names. These are keys for <see cref="Hands"/>. The order of this list determines the order in which hands are iterated over.
    /// </summary>
    [DataField]
    public List<string> SortedHands = new();

    /// <summary>
    ///     If true, the items in the hands won't be affected by explosions.
    /// </summary>
    [DataField]
    public bool DisableExplosionRecursion;

    /// <summary>
    ///     Modifies the speed at which items are thrown.
    /// </summary>
    [DataField]
    public float BaseThrowspeed = 11f;

    /// <summary>
    ///     Distance after which longer throw targets stop increasing throw impulse.
    /// </summary>
    [DataField]
    public float ThrowRange = 8f;

    /// <summary>
    ///     Whether or not to add in-hand sprites for held items. Some entities (e.g., drones) don't want these.
    ///     Used by the client.
    /// </summary>
    [DataField]
    public bool ShowInHands = true;

    /// <summary>
    ///     Data about the current sprite layers that the hand is contributing to the owner entity. Used for sprite in-hands.
    ///     Used by the client.
    /// </summary>
    public readonly Dictionary<HandLocation, HashSet<string>> RevealedLayers = new();

    /// <summary>
    ///     The time at which throws will be allowed again.
    /// </summary>
    [DataField, AutoPausedField]
    public TimeSpan NextThrowTime;

    /// <summary>
    ///     The minimum time inbetween throws.
    /// </summary>
    [DataField]
    public TimeSpan ThrowCooldown = TimeSpan.FromSeconds(0.5f);

    /// <summary>
    ///     Fallback displacement map applied to all sprites in the hand, unless otherwise specified
    /// </summary>
    [DataField]
    public DisplacementData? HandDisplacement;

    /// <summary>
    ///     If defined, applies to all sprites in the left hand, ignoring <see cref="HandDisplacement"/>
    /// </summary>
    [DataField]
    public DisplacementData? LeftHandDisplacement;

    /// <summary>
    ///     If defined, applies to all sprites in the right hand, ignoring <see cref="HandDisplacement"/>
    /// </summary>
    [DataField]
    public DisplacementData? RightHandDisplacement;

    /// <summary>
    /// If false, hands cannot be stripped, and they do not show up in the stripping menu.
    /// </summary>
    [DataField]
    public bool CanBeStripped = true;
}

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

[Serializable, NetSerializable]
public sealed class HandsComponentState : ComponentState
{
    public readonly Dictionary<string, Hand> Hands;
    public readonly List<string> SortedHands;
    public readonly string? ActiveHandId;

    public HandsComponentState(HandsComponent handComp)
    {
        // cloning lists because of test networking.
        Hands = new(handComp.Hands);
        SortedHands = new(handComp.SortedHands);
        ActiveHandId = handComp.ActiveHandId;
    }
}

/// <summary>
///     What side of the body this hand is on.
/// </summary>
/// <seealso cref="HandUILocation"/>
/// <seealso cref="HandLocationExt"/>
public enum HandLocation : byte
{
    Left,
    Middle,
    Right
}

/// <summary>
/// What side of the UI a hand is on.
/// </summary>
/// <seealso cref="HandLocationExt"/>
/// <seealso cref="HandLocation"/>
public enum HandUILocation : byte
{
    Left,
    Right
}

/// <summary>
/// Helper functions for working with <see cref="HandLocation"/>.
/// </summary>
public static class HandLocationExt
{
    /// <summary>
    /// Convert a <see cref="HandLocation"/> into the appropriate <see cref="HandUILocation"/>.
    /// This maps "middle" hands to <see cref="HandUILocation.Right"/>.
    /// </summary>
    public static HandUILocation GetUILocation(this HandLocation location)
    {
        return location switch
        {
            HandLocation.Left => HandUILocation.Left,
            HandLocation.Middle => HandUILocation.Right,
            HandLocation.Right => HandUILocation.Right,
            _ => throw new ArgumentOutOfRangeException(nameof(location), location, null)
        };
    }
}
