using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Sound;
using Content.Shared.Tools.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Tools.Components
{
    [RegisterComponent]
    public class WelderComponent : SharedWelderComponent
    {
        /// <summary>
        ///     Solution on the entity that contains the fuel.
        /// </summary>
        [DataField("fuelSolution")]
        public string FuelSolution { get; } = "Welder";

        /// <summary>
        ///     Reagent that will be used as fuel for welding.
        /// </summary>
        [DataField("fuelReagent", customTypeSerializer:typeof(PrototypeIdSerializer<ReagentPrototype>))]
        public string FuelReagent { get; } = "WeldingFuel";

        /// <summary>
        ///     Fuel consumption per second, while the welder is active.
        /// </summary>
        [DataField("fuelConsumption")]
        public FixedPoint2 FuelConsumption { get; } = FixedPoint2.New(0.05f);

        /// <summary>
        ///     A fuel amount to be consumed when the welder goes from being unlit to being lit.
        /// </summary>
        [DataField("welderOnConsume")]
        public FixedPoint2 FuelLitCost { get; } = FixedPoint2.New(0.5f);

        /// <summary>
        ///     Sound played when the welder is turned off.
        /// </summary>
        [DataField("welderOffSounds")]
        public SoundSpecifier WelderOffSounds { get; } = new SoundCollectionSpecifier("WelderOff");

        /// <summary>
        ///     Sound played when the tool is turned on.
        /// </summary>
        [DataField("welderOnSounds")]
        public SoundSpecifier WelderOnSounds { get; } = new SoundCollectionSpecifier("WelderOn");

        [DataField("welderRefill")]
        public SoundSpecifier WelderRefill { get; } = new SoundPathSpecifier("/Audio/Effects/refill.ogg");
    }
}
