using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Tools.Components;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.Tools.Components
{
    [RegisterComponent]
    public sealed partial class WelderComponent : SharedWelderComponent
    {
        /// <summary>
        ///     Name of <see cref="FuelSolution"/>.
        /// </summary>
        [DataField("fuelSolution"), ViewVariables(VVAccess.ReadWrite)]
        public string FuelSolutionName = "Welder";

        /// <summary>
        ///     Solution on the entity that contains the fuel.
        /// </summary>
        [DataField("fuelSolutionRef")]
        public Entity<SolutionComponent>? FuelSolution = null;

        /// <summary>
        ///     Reagent that will be used as fuel for welding.
        /// </summary>
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public ProtoId<ReagentPrototype> FuelReagent = "WeldingFuel";

        /// <summary>
        ///     Fuel consumption per second while the welder is active.
        /// </summary>
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public FixedPoint2 FuelConsumption = FixedPoint2.New(2.0f);

        /// <summary>
        ///     A fuel amount to be consumed when the welder goes from being unlit to being lit.
        /// </summary>
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public FixedPoint2 FuelLitCost = FixedPoint2.New(0.5f);

        /// <summary>
        ///     Sound played when refilling the welder.
        /// </summary>
        [DataField]
        public SoundSpecifier WelderRefill = new SoundPathSpecifier("/Audio/Effects/refill.ogg");

        /// <summary>
        ///     Whether the item is safe to refill while lit without exploding the tank.
        /// </summary>
        [DataField]
        public bool TankSafe = false; //I have no idea what I'm doing

    }
}
