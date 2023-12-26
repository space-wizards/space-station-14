using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Paint;

[RegisterComponent, NetworkedComponent]
[Access(typeof(PaintSystem))]
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
    /// Reagent that will be used as paint.
    /// </summary>
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<ReagentPrototype>))]
    public string Reagent = "SpaceGlue";


    /// <summary>
    /// Color that the painting entity will insruct the painted entity to be.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public Color Color = Color.BlueViolet;

    /// <summary>
    /// Reagent consumption per use.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 ConsumptionUnit = FixedPoint2.New(5);

    /// <summary>
    /// Duration per unit
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan DurationPerUnit = TimeSpan.FromSeconds(6);
}
