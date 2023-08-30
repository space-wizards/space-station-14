using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Tools.Components;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Tools.Components
{
    [RegisterComponent]
    public sealed partial class WelderComponent : SharedWelderComponent
    {
        /// <summary>
        ///     Solution on the entity that contains the fuel.
        /// </summary>
        [DataField("fuelSolution"), ViewVariables(VVAccess.ReadWrite)]
        public string FuelSolution { get; private set; } = "Welder";

        /// <summary>
        ///     Reagent that will be used as fuel for welding.
        /// </summary>
        [DataField("fuelReagent", customTypeSerializer:typeof(PrototypeIdSerializer<ReagentPrototype>)), ViewVariables(VVAccess.ReadWrite)]
        public string FuelReagent { get; private set; } = "WeldingFuel";

        /// <summary>
        ///     Fuel consumption per second, while the welder is active.
        /// </summary>
        [DataField("fuelConsumption"), ViewVariables(VVAccess.ReadWrite)]
        public FixedPoint2 FuelConsumption { get; private set; } = FixedPoint2.New(2.0f);

        /// <summary>
        ///     A fuel amount to be consumed when the welder goes from being unlit to being lit.
        /// </summary>
        [DataField("fuelLitCost"), ViewVariables(VVAccess.ReadWrite)]
        public FixedPoint2 FuelLitCost { get; private set; } = FixedPoint2.New(0.5f);

        /// <summary>
        ///     Sound played when the welder is turned off.
        /// </summary>
        [DataField("welderOffSounds")]
        public SoundSpecifier WelderOffSounds { get; private set; } = new SoundCollectionSpecifier("WelderOff");

        /// <summary>
        ///     Sound played when the tool is turned on.
        /// </summary>
        [DataField("welderOnSounds")]
        public SoundSpecifier WelderOnSounds { get; private set; } = new SoundCollectionSpecifier("WelderOn");

        [DataField("welderRefill")]
        public SoundSpecifier WelderRefill { get; private set; } = new SoundPathSpecifier("/Audio/Effects/refill.ogg");

        /// <summary>
        ///     When the welder is lit, this damage is added to the base melee weapon damage.
        /// </summary>
        /// <remarks>
        ///     If this is a standard welder, this damage bonus should probably subtract the entity's standard melee weapon damage
        ///     and replace it all with heat damage.
        /// </remarks>
        [DataField("litMeleeDamageBonus")]
        public DamageSpecifier LitMeleeDamageBonus = new();

        /// <summary>
        ///     Whether the item is safe to refill while lit without exploding the tank.
        /// </summary>
        [DataField("tankSafe")]
        public bool TankSafe = false; //I have no idea what I'm doing

    }
}
