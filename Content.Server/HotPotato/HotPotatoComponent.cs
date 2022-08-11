using Robust.Shared.Audio;

namespace Content.Server.HotPotato
{
    /// <summary>
    /// This component will turn items into a hot potato! When they are activated they cannot be dropped.
    /// You can get rid of it by clicking on another player
    /// </summary>
    [RegisterComponent]
    public sealed class HotPotatoComponent : Component
    {
        public bool IsActivated = false;

        /// <summary>
        /// Whether or not the hot potato is a dud
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("isDud")]
        public bool IsDud = false;

        /// <summary>
        /// For dud potatos, Item the dud will turn into after it's timer has run out
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("turnInto")]
        public string TurnInto = "FoodMealPotatoLoaded";

        /// <summary>
        /// For dud potatos, Item the dud will turn into after it's timer has run out
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)] [DataField("dudSound")]
        public SoundSpecifier DudSound = new SoundPathSpecifier("/Audio/Effects/desecration-01.ogg");

        /// <summary>
        /// The cooldown in between attempts to pass the potato, mostly here so they don't spam click everything
        /// </summary>
        public TimeSpan LastUseTime;
        public TimeSpan CooldownEnd;
        [DataField("cooldownTime")]
        public float CooldownTime { get; } = 1.5f;

    }
}
