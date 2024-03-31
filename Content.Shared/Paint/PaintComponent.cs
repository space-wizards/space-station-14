using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;

namespace Content.Shared.Paint;

/// <summary>
/// Entity when used on another entity will paint target entity.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedPaintSystem))]
public sealed partial class PaintComponent : Component
{
    /// <summary>
    /// Noise made when paint applied.
    /// </summary>
    [DataField]
    public SoundSpecifier Spray = new SoundPathSpecifier("/Audio/Effects/spray2.ogg");

    /// <summary>
    /// Solution on the entity that contains the paint.
    /// </summary>
    [DataField]
    public string Solution = "drink";

    /// <summary>
    /// How long the doafter will take.
    /// </summary>
    [DataField]
    public int Delay = 2;

    /// <summary>
    /// Reagent that will be used as paint.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<ReagentPrototype> Reagent = "SpaceGlue";

    /// <summary>
    /// Color that the painting entity will instruct the painted entity to be.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Color Color = Color.FromHex("#c62121");

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public EntityWhitelist? Blacklist;
    /// <summary>
    /// Reagent consumption per use.
    /// </summary>
    [DataField]
    public FixedPoint2 ConsumptionUnit = FixedPoint2.New(5);

    /// <summary>
    /// Duration per unit
    /// </summary>
    [DataField]
    public TimeSpan DurationPerUnit = TimeSpan.FromSeconds(6);
}
