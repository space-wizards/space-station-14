using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Fluids;

/// <summary>
/// For entities that can clean up puddles
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed class AbsorbentComponent : Component
{
    public const string SolutionName = "absorbed";

    [DataField("progress")]
    public Dictionary<Color, float> Progress = new();

    /// <summary>
    /// How much solution we can transfer in one interaction.
    /// </summary>
    [DataField("pickupAmount")]
    public FixedPoint2 PickupAmount = FixedPoint2.New(50);

    [DataField("pickupSound")]
    public SoundSpecifier PickupSound = new SoundPathSpecifier("/Audio/Effects/Fluids/slosh.ogg");

    [DataField("transferSound")]
    public SoundSpecifier TransferSound = new SoundPathSpecifier("/Audio/Effects/Fluids/watersplash.ogg");
}
