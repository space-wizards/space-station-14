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
    ///     The amount of throw impulse per distance the player is from the throw target.
    /// </summary>
    [DataField("throwForceMultiplier")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float ThrowForceMultiplier { get; set; } = 10f; //should be tuned so that a thrown item lands about under the player's cursor

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
public enum HandLocation : byte
{
    Left,
    Middle,
    Right
}
