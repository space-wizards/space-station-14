using Content.Shared.Waypointer.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;
using Robust.Shared.Utility;

namespace Content.Shared.Waypointer;

/// <summary>
/// This is the prototype for a waypointer.
/// This is stored in either <see cref="Components.WaypointerComponent"/> or <see cref="Components.ClothingShowWaypointerComponent"/>.
/// It's responsible for defining what kind of waypointer is shown to the client.
/// </summary>
[Prototype]
public sealed partial class WaypointerPrototype : IPrototype, IInheritingPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <inheritdoc/>
    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<WaypointerPrototype>))]
    public string[]? Parents { get; private set; }

    /// <inheritdoc/>
    [AbstractDataField, NeverPushInheritance]
    public bool Abstract { get; private set; }

    [DataField(required: true)]
    public ComponentRegistry TrackedComponents = default!;

    /// <summary>
    /// The path to the rsi folder.
    /// </summary>
    [DataField(required: true)]
    public ResPath RsiPath;

    /// <summary>
    /// This signifies how many states the waypointer has.
    /// These are used to show distance to the tracked target.
    /// </summary>
    [DataField]
    public float WaypointerStates = 1f;

    /// <summary>
    /// The color of the waypointer.
    /// Only works on properly grey-scaled textures.
    /// </summary>
    [DataField]
    public Color? Color;

    /// <summary>
    /// Whether the waypointer is active on grid.
    /// </summary>
    [DataField]
    public bool WorkOnGrid;

    /// <summary>
    /// Whether the waypointer is active when the entity is in combat.
    /// </summary>
    [DataField]
    public bool WorkInCombat;

    /// <summary>
    /// The maximum range to where the pinpointer can track something.
    /// </summary>
    [DataField]
    public int MaxRange = 200;

    /// <summary>
    /// The whitelist that the entity needs to fulfill to be tracked.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// The whitelist that the entity needs to fail to be tracked.
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;
}
