using Content.Shared.Wires;

namespace Content.Server.Wires;

/// <summary>
///     An interface used by WiresSystem to allow compositional wiresets.
///     This is expected to be flyweighted, do not store per-entity state
///     within an object/class that implements IWireAction.
/// </summary>
public interface IWireAction
{
    /// <summary>
    ///     This is to link the wire's status with
    ///     its corresponding UI key. If this is null,
    ///     GetStatusLightData MUST also return null,
    ///     otherwise nothing happens.
    /// </summary>
    public object? StatusKey { get; }

    /// <summary>
    ///     Called when the wire in the layout
    ///     is created for the first time. Ensures
    ///     that the referenced action has all
    ///     the correct system references (plus
    ///     other information if needed,
    ///     but wire actions should NOT be stateful!)
    /// </summary>
    public void Initialize();

    /// <summary>
    ///     Called when a wire is finally processed
    ///     by WiresSystem upon wire layout
    ///     creation. Use this to set specific details
    ///     about the state of the entity in question.
    ///
    ///     If this returns false, this will convert
    ///     the given wire into a 'dummy' wire instead.
    /// </summary>
    /// <param name="wire">The wire in the entity's WiresComponent.</param>
    /// <param name="count">The current count of this instance of the wire type.</param>
    public bool AddWire(Wire wire, int count);

    /// <summary>
    ///     What happens when this wire is cut. If this returns false, the wire will not actually get cut.
    /// </summary>
    /// <param name="user">The user attempting to interact with the wire.</param>
    /// <param name="wire">The wire being interacted with.</param>
    /// <returns>true if successful, false otherwise.</returns>
    public bool Cut(EntityUid user, Wire wire);

    /// <summary>
    ///     What happens when this wire is mended. If this returns false, the wire will not actually get cut.
    /// </summary>
    /// <param name="user">The user attempting to interact with the wire.</param>
    /// <param name="wire">The wire being interacted with.</param>
    /// <returns>true if successful, false otherwise.</returns>
    public bool Mend(EntityUid user, Wire wire);

    /// <summary>
    ///     This method gets called when the wire is pulsed..
    /// </summary>
    /// <param name="user">The user attempting to interact with the wire.</param>
    /// <param name="wire">The wire being interacted with.</param>
    public void Pulse(EntityUid user, Wire wire);

    /// <summary>
    ///     Used when a wire's state on an entity needs to be updated.
    ///     Mostly for things related to entity events, e.g., power.
    /// </summary>
    public void Update(Wire wire);

    /// <summary>
    ///     Used for when WiresSystem requires the status light data
    ///     for display on the client.
    /// </summary>
    /// <returns>StatusLightData to display light data, null to have no status light.</returns>
    public StatusLightData? GetStatusLightData(Wire wire);
}
