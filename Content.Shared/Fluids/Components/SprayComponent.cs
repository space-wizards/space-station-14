using Content.Shared.FixedPoint;
using Content.Shared.Fluids.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Fluids.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SpraySystem))]
public sealed partial class SprayComponent : Component
{
    public const string SolutionName = "spray";

    /// <summary>
    /// How much solution is transferred per spray.
    /// </summary>
    [DataField]
    public FixedPoint2 TransferAmount = 10;

    /// <summary>
    /// How far the spray can reach.
    /// </summary>
    [DataField]
    public float SprayDistance = 3.5f;

    /// <summary>
    /// How fast the spray travels.
    /// </summary>
    [DataField]
    public float SprayVelocity = 3.5f;

    /// <summary>
    /// The prototype to spawn for each vapor cloud.
    /// </summary>
    [DataField]
    public EntProtoId SprayedPrototype = "Vapor";

    /// <summary>
    /// How many vapor clouds to spawn.
    /// </summary>
    [DataField]
    public int VaporAmount = 1;

    /// <summary>
    /// How spread out the vapor clouds are.
    /// </summary>
    [DataField]
    public float VaporSpread = 90f;

    /// <summary>
    /// How much the player is pushed back for each spray.
    /// </summary>
    [DataField]
    public float PushbackAmount = 5f;

    /// <summary>
    /// The sound to play when spraying.
    /// </summary>
    [DataField(required: true)]
    [Access(typeof(SpraySystem), Other = AccessPermissions.ReadExecute)] // FIXME Friends
    public SoundSpecifier SpraySound { get; private set; } = default!;

    /// <summary>
    /// The popup message to show when the spray is empty.
    /// </summary>
    [DataField]
    public LocId SprayEmptyPopupMessage = "spray-component-is-empty-message";
}
