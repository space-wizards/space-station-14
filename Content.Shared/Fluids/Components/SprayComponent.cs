using Content.Shared.FixedPoint;
using Content.Shared.Fluids.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.Fluids.Components;

[RegisterComponent]
[Access(typeof(SharedSpraySystem))]
public sealed partial class SprayComponent : Component
{
    [DataField]
    public string Solution = "spray";

    [DataField]
    public FixedPoint2 TransferAmount = 10;

    [DataField]
    public float SprayDistance = 3.5f;

    [DataField]
    public float SprayVelocity = 3.5f;

    [DataField]
    public EntProtoId SprayedPrototype = "Vapor";

    [DataField]
    public int VaporAmount = 1;

    [DataField]
    public float VaporSpread = 90f;

    /// <summary>
    /// How much the player is pushed back for each spray.
    /// </summary>
    [DataField]
    public float PushbackAmount = 5f;

    [DataField(required: true)]
    [Access(typeof(SharedSpraySystem), Other = AccessPermissions.ReadExecute)] // FIXME Friends
    public SoundSpecifier SpraySound { get; private set; } = default!;

    [DataField]
    public LocId SprayEmptyPopupMessage = "spray-component-is-empty-message";
}
