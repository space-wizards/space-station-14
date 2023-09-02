using Content.Server.Nutrition.EntitySystems;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;
using Robust.Shared.Audio;

namespace Content.Server.Nutrition.Components
{
    [RegisterComponent]
    [Access(typeof(DrinkSystem))]
    public sealed partial class DrinkComponent : Component
    {
        [DataField("solution")]
        public string SolutionName { get; set; } = DefaultSolutionName;
        public const string DefaultSolutionName = "drink";

        [DataField("useSound")]
        public SoundSpecifier UseSound = new SoundPathSpecifier("/Audio/Items/drink.ogg");

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("transferAmount")]
        public FixedPoint2 TransferAmount { get; [UsedImplicitly] private set; } = FixedPoint2.New(5);

        /// <summary>
        /// How long it takes to drink this yourself.
        /// </summary>
        [DataField("delay")]
        public float Delay = 1;

        [DataField("examinable")]
        public bool Examinable = true;

        /// <summary>
        ///     This is how many seconds it takes to force feed someone this drink.
        /// </summary>
        [DataField("forceFeedDelay")]
        public float ForceFeedDelay = 3;
    }
}
