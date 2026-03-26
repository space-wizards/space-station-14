using Content.Server.StationEvents.Events;
using Content.Shared.EntityList;
using Content.Shared.Physics;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.StationEvents.Components;

/// <summary>
/// Spawns several banned books in various strange, out-of-the-way places on the station, intended to be hunted down by the librarian.
/// </summary>
[RegisterComponent, Access(typeof(BookHuntVariationPassRule))]
public sealed partial class BookHuntVariationPassRuleComponent : Component
{
    /// <summary>
    /// The books that will be spawned by various methods.
    /// </summary>
    [DataField(required: true)]
    public List<EntProtoId> BookPrototypes = [];

    /// <summary>
    /// Prototype for the smuggler's satchel.
    /// </summary>
    [DataField]
    public EntProtoId SmugglerSatchel = "ClothingBackpackSatchelSmuggler";

    /// <summary>
    /// Tag that defines the "bookshelves" that the book hunt spawns a book in.
    /// </summary>
    [DataField]
    public ProtoId<TagPrototype> BookshelfTag = "BookHuntBookshelf";

    /// <summary>
    /// Tag that defines the "dresser" that the book hunt spawns a book in.
    /// </summary>
    [DataField]
    public ProtoId<TagPrototype> DresserTag = "BookHuntDresser";

    /// <summary>
    /// Tag that defines the "suit storage" that the book hunt spawns a book in.
    /// </summary>
    [DataField]
    public ProtoId<TagPrototype> SuitStorageTag = "BookHuntSuitStorage";

    /// <summary>
    /// Prototype for the duffel.
    /// </summary>
    [DataField]
    public EntProtoId Duffel = "ClothingBackpackDuffel";

    /// <summary>
    /// Buffer distance so that the space ray point doesn't ever spawn inside the station
    /// </summary>
    [DataField]
    public float SpawnBuffer = 20f;

    /// <summary>
    /// The collision mask the space ray uses to collide with the walls and doors on the outside of the station.
    /// </summary>
    [DataField]
    public CollisionGroup CollisionMask = CollisionGroup.SingularityLayer;

    /// <summary>
    /// How far away does the duffel spawn from the ray collision
    /// </summary>
    [DataField]
    public float HitDistance = 1.0f;
}
