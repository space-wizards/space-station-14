using System.Threading;

namespace Content.Server.Botany
{
    /// <summary>
    /// Anything that can be used to cross-pollinate plants.
    /// </summary>
    [RegisterComponent]
    public sealed partial class BotanySwabComponent : Component
    {
        [DataField("swabDelay")]
        public float SwabDelay = 2f;

        /// <summary>
        /// SeedData from the first plant that got swabbed.
        /// </summary>
        public SeedData? SeedData;
    }
}
