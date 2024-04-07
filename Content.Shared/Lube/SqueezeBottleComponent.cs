// using Content.Shared.Chemistry.Reagent;
// using Content.Shared.FixedPoint;
// using Robust.Shared.Audio;
// using Robust.Shared.GameStates;
// using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

// namespace Content.Shared.Lube;

// [RegisterComponent, NetworkedComponent]
// public sealed partial class SqueezeBottleComponent : Component
// {
//     [DataField("OnSqueezeNoise")]
//     public SoundSpecifier Squeeze = new SoundPathSpecifier("/Audio/Items/squeezebottle.ogg");

//     /// <summary>
//     /// Solution on the entity that contains the glue.
//     /// </summary>
//     [DataField("solution")]
//     public string Solution = "drink";

//     /// <summary>
//     /// Reagent that will be used as glue.
//     /// </summary>
//     [DataField("reagent", customTypeSerializer: typeof(PrototypeIdSerializer<ReagentPrototype>))]
//     public string Reagent = "SpaceLube";

//     /// <summary>
//     /// Reagent consumption per use.
//     /// </summary>
//     [DataField("AmountConsumedOnUse"), ViewVariables(VVAccess.ReadWrite)]
//     public FixedPoint2 AmountConsumedOnUse = FixedPoint2.New(3);
// }
