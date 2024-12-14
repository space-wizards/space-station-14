using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Nutrition.Components;

[NetworkedComponent, AutoGenerateComponentState]
[RegisterComponent, Access(typeof(SharedDrinkSystem))]
public sealed partial class DrinkComponent : Component
{
    [DataField]
    public string Solution = "drink";

    [DataField, AutoNetworkedField]
    public SoundSpecifier UseSound = new SoundPathSpecifier("/Audio/Items/drink.ogg");

    [DataField, AutoNetworkedField]
    public FixedPoint2 TransferAmount = FixedPoint2.New(5);

    /// <summary>
    /// How long it takes to drink this yourself.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Delay = 1;

    [DataField, AutoNetworkedField]
    public bool Examinable = true;

    /// <summary>
    /// If true, trying to drink when empty will not handle the event.
    /// This means other systems such as equipping on use can run.
    /// Example usecase is the bucket.
    /// </summary>
    [DataField]
    public bool IgnoreEmpty;

    /// <summary>
    ///     This is how many seconds it takes to force feed someone this drink.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ForceFeedDelay = 3;
}
