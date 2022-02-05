using System;
using System.Threading;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Nutrition.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Sound;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.ViewVariables;

namespace Content.Server.Nutrition.Components
{
    [RegisterComponent, Friend(typeof(FoodSystem))]
    public class FoodComponent : Component
    {
        [DataField("solution")]
        public string SolutionName { get; set; } = "food";

        [ViewVariables]
        [DataField("useSound")]
        public SoundSpecifier UseSound { get; set; } = new SoundPathSpecifier("/Audio/Items/eatfood.ogg");

        [ViewVariables]
        [DataField("trash", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string? TrashPrototype { get; set; }

        [ViewVariables]
        [DataField("transferAmount")]
        public FixedPoint2? TransferAmount { get; set; } = FixedPoint2.New(5);

        /// <summary>
        /// Acceptable utensil to use
        /// </summary>
        [DataField("utensil")]
        public UtensilType Utensil = UtensilType.Fork; //There are more "solid" than "liquid" food

        /// <summary>
        /// Is utensil required to eat this food
        /// </summary>
        [DataField("utensilRequired")]
        public bool UtensilRequired = false;

        [DataField("eatMessage")]
        public string EatMessage = "food-nom";

        /// <summary>
        ///     This is how many seconds it takes to force feed someone this food.
        ///     Should probably be smaller for small items like pills.
        /// </summary>
        [DataField("forceFeedDelay")]
        public float ForceFeedDelay = 3;

        /// <summary>
        ///     Token for interrupting a do-after action (e.g., force feeding). If not null, implies component is
        ///     currently "in use".
        /// </summary>
        public CancellationTokenSource? CancelToken;

        [ViewVariables]
        public int UsesRemaining
        {
            get
            {
                if (!EntitySystem.Get<SolutionContainerSystem>().TryGetSolution(Owner, SolutionName, out var solution))
                {
                    return 0;
                }

                if (TransferAmount == null)
                    return solution.CurrentVolume == 0 ? 0 : 1;

                return solution.CurrentVolume == 0
                    ? 0
                    : Math.Max(1, (int) Math.Ceiling((solution.CurrentVolume / (FixedPoint2)TransferAmount).Float()));
            }
        }
    }
}
