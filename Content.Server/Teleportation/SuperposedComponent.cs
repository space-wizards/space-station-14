namespace Content.Server.Teleportation;

/// <summary>
/// Used to make items "superposed".
/// When dropped, superposed items saves EntityUids of nearest PlaceableSuraceComponent
/// (like tables) and hop between them when not directly looked at.
/// </summary>
[RegisterComponent, Access(typeof(SuperposedSystem))]
public sealed partial class SuperposedComponent : Component
{
    /// <summary>
    /// When some potential observer is closer than MinObserverRange to the superposed entity,
    /// the entity cannot change their position.
    /// </summary>
    [DataField]
    public float MinObserverRange = 2f;

    /// <summary>
    /// Maximum range to the potential observer. Entities outside the range are not considered
    // as potential observers.
    /// </summary>
    [DataField]
    public float MaxObserverRange = 5f;

    /// <summary>
    /// In what range can the object hop around the place where it was dropped.
    /// </summary>
    [DataField]
    public float SuperposeRange = 2f;

    /// <summary>
    /// Max offset, applied to the entity position after each hop.
    /// </summary>
    [DataField]
    public float MaxOffset = 0.25f;

    /// <summary>
    /// Server-side flag that tells whether the entity was observed in this position.
    /// If superposed entity wasn't observed, it won't change its position.
    /// </summary>
    [DataField]
    public bool Observed = false;

    /// <summary>
    /// List of possible hop locations constructed when the superposed entity is dropped.
    /// </summary>
    [DataField]
    public EntityUid[] PossibleLocations = {};
}
