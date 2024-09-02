using Content.Server.Polymorph.Systems;
using Content.Shared.Polymorph;

namespace Content.Server.Polymorph.Components;

[RegisterComponent]
[Access(typeof(PolymorphSystem))]
public sealed partial class PolymorphedEntityComponent : Component
{
    /// <summary>
    /// The polymorph prototype, used to track various information
    /// about the polymorph
    /// </summary>
    [DataField(required: true)]
    public PolymorphConfiguration Configuration = new();

    /// <summary>
    /// The original entity that the player will revert back into
    /// </summary>
    [DataField(required: true)]
    public EntityUid Parent;

    /// <summary>
    /// The amount of time that has passed since the entity was created
    /// used for tracking the duration
    /// </summary>
    [DataField]
    public float Time;

    [DataField]
    public EntityUid? Action;
}
