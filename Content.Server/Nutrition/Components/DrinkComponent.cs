using System.Threading;
using Content.Server.Nutrition.EntitySystems;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;
using Robust.Shared.Audio;

namespace Content.Server.Nutrition.Components
{
    [RegisterComponent]
    [Access(typeof(DrinkSystem))]
    public sealed class DrinkComponent : Component
    {
        [DataField("solution")]
        public string SolutionName { get; set; } = DefaultSolutionName;
        public const string DefaultSolutionName = "drink";

        [DataField("useSound")]
        public SoundSpecifier UseSound = new SoundPathSpecifier("/Audio/Items/drink.ogg");

        [DataField("isOpen")]
        internal bool DefaultToOpened;

        [ViewVariables(VVAccess.ReadWrite)]
        public FixedPoint2 TransferAmount { get; [UsedImplicitly] private set; } = FixedPoint2.New(5);

        [ViewVariables(VVAccess.ReadWrite)]
        public bool Opened;

        [DataField("openSounds")]
        public SoundSpecifier OpenSounds = new SoundCollectionSpecifier("canOpenSounds");

        [DataField("pressurized")]
        public bool Pressurized;

        [DataField("burstSound")]
        public SoundSpecifier BurstSound = new SoundPathSpecifier("/Audio/Effects/flash_bang.ogg");

        /// <summary>
        /// How long it takes to drink this yourself.
        /// </summary>
        [DataField("delay")]
        public float Delay = 1;

        /// <summary>
        ///     This is how many seconds it takes to force feed someone this drink.
        /// </summary>
        [DataField("forceFeedDelay")]
        public float ForceFeedDelay = 3;

        /// <summary>
        ///     Token for interrupting a do-after action (e.g., force feeding). If not null, implies component is
        ///     currently "in use".
        /// </summary>
        public CancellationTokenSource? CancelToken;
    }
}
