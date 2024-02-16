using Content.Server.Nutrition.EntitySystems;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;

namespace Content.Server.Nutrition.Components;

[RegisterComponent, Access(typeof(DrinkSystem))]
public sealed partial class DrinkComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string Solution = "drink";

    [DataField]
    public SoundSpecifier UseSound = new SoundPathSpecifier("/Audio/Items/drink.ogg");

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 TransferAmount = FixedPoint2.New(5);

    /// <summary>
    /// How long it takes to drink this yourself.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float Delay = 1;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool Examinable = true;

    /// <summary>
    /// If true, trying to drink when empty will not handle the event.
    /// This means other systems such as equipping on use can run.
    /// Example usecase is the bucket.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool IgnoreEmpty;

    /// <summary>
    ///     This is how many seconds it takes to force feed someone this drink.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float ForceFeedDelay = 3;
}
