using Content.Server.Buckle.Systems;
using Content.Shared.Alert;
using Content.Shared.Buckle.Components;
using Robust.Shared.Audio;

namespace Content.Server.Buckle.Components;

[RegisterComponent]
[ComponentReference(typeof(SharedStrapComponent))]
[Access(typeof(BuckleSystem))]
public sealed class StrapComponent : SharedStrapComponent
{
    /// <summary>
    /// The angle in degrees to rotate the player by when they get strapped
    /// </summary>
    [DataField("rotation")]
    public int Rotation { get; set; }

    /// <summary>
    /// The size of the strap which is compared against when buckling entities
    /// </summary>
    [DataField("size")]
    public int Size { get; set; } = 100;

    /// <summary>
    /// If disabled, nothing can be buckled on this object, and it will unbuckle anything that's already buckled
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// You can specify the offset the entity will have after unbuckling.
    /// </summary>
    [DataField("unbuckleOffset", required: false)]
    public Vector2 UnbuckleOffset = Vector2.Zero;
    /// <summary>
    /// The sound to be played when a mob is buckled
    /// </summary>
    [DataField("buckleSound")]
    public SoundSpecifier BuckleSound { get; } = new SoundPathSpecifier("/Audio/Effects/buckle.ogg");

    /// <summary>
    /// The sound to be played when a mob is unbuckled
    /// </summary>
    [DataField("unbuckleSound")]
    public SoundSpecifier UnbuckleSound { get; } = new SoundPathSpecifier("/Audio/Effects/unbuckle.ogg");

    /// <summary>
    /// ID of the alert to show when buckled
    /// </summary>
    [DataField("buckledAlertType")]
    public AlertType BuckledAlertType { get; } = AlertType.Buckled;

    /// <summary>
    /// The sum of the sizes of all the buckled entities in this strap
    /// </summary>
    public int OccupiedSize { get; set; }
}
