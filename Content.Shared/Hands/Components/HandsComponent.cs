using Content.Shared.DisplacementMap;
using Content.Shared.Hands.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Hands.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
[Access(typeof(SharedHandsSystem))]
public sealed partial class HandsComponent : Component
{
    /// <summary>
    ///     The currently active hand.
    /// </summary>
    [ViewVariables]
    public Hand? ActiveHand;

    /// <summary>
    ///     The item currently held in the active hand.
    /// </summary>
    [ViewVariables]
    public EntityUid? ActiveHandEntity => ActiveHand?.HeldEntity;

    [ViewVariables]
    public Dictionary<string, Hand> Hands = new();

    public int Count => Hands.Count;

    /// <summary>
    ///     List of hand-names. These are keys for <see cref="Hands"/>. The order of this list determines the order in which hands are iterated over.
    /// </summary>
    public List<string> SortedHands = new();

    /// <summary>
    ///     If true, the items in the hands won't be affected by explosions.
    /// </summary>
    [DataField]
    public bool DisableExplosionRecursion = false;

    /// <summary>
    ///     Modifies the speed at which items are thrown.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public float BaseThrowspeed { get; set; } = 11f;

    /// <summary>
    ///     Distance after which longer throw targets stop increasing throw impulse.
    /// </summary>
    [DataField("throwRange")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float ThrowRange { get; set; } = 8f;

    /// <summary>
    ///     Whether or not to add in-hand sprites for held items. Some entities (e.g., drones) don't want these.
    ///     Used by the client.
    /// </summary>
    [DataField("showInHands")]
    public bool ShowInHands = true;

    /// <summary>
    ///     Data about the current sprite layers that the hand is contributing to the owner entity. Used for sprite in-hands.
    ///     Used by the client.
    /// </summary>
    public readonly Dictionary<HandLocation, HashSet<string>> RevealedLayers = new();

    /// <summary>
    ///     The time at which throws will be allowed again.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    [AutoPausedField]
    public TimeSpan NextThrowTime;

    /// <summary>
    ///     The minimum time inbetween throws.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan ThrowCooldown = TimeSpan.FromSeconds(0.5f);

    [DataField]
    public DisplacementData? HandDisplacement;

    /// <summary>
    /// If false, hands cannot be stripped, and they do not show up in the stripping menu.
    /// </summary>
    [DataField]
    public bool CanBeStripped = true;
}

[Serializable, NetSerializable]
public sealed class Hand //TODO: This should definitely be a struct - Jezi
{
    [ViewVariables]
    public string Name { get; }

    [ViewVariables]
    public HandLocation Location { get; }

    /// <summary>
    ///     The container used to hold the contents of this hand. Nullable because the client must get the containers via <see cref="ContainerManagerComponent"/>,
    ///     which may not be synced with the server when the client hands are created.
    /// </summary>
    [ViewVariables, NonSerialized]
    public ContainerSlot? Container;

    [ViewVariables]
    public EntityUid? HeldEntity => Container?.ContainedEntity;

    public bool IsEmpty => HeldEntity == null;

    public Hand(string name, HandLocation location, ContainerSlot? container = null)
    {
        Name = name;
        Location = location;
        Container = container;
    }
}

[Serializable, NetSerializable]
public sealed class HandsComponentState : ComponentState
{
    public readonly List<Hand> Hands;
    public readonly List<string> HandNames;
    public readonly string? ActiveHand;

    public HandsComponentState(HandsComponent handComp)
    {
        // cloning lists because of test networking.
        Hands = new(handComp.Hands.Values);
        HandNames = new(handComp.SortedHands);
        ActiveHand = handComp.ActiveHand?.Name;
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
