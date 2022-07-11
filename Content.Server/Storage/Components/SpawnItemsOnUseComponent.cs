using Content.Shared.Storage;
using Robust.Shared.Audio;

namespace Content.Server.Storage.Components
{
    /// <summary>
    ///     Spawns items when used in hand.
    /// </summary>
    [RegisterComponent]
    public sealed class SpawnItemsOnUseComponent : Component
    {
        /// <summary>
        ///     The list of entities to spawn, with amounts and orGroups.
        /// </summary>
        /// <returns></returns>
        [DataField("items", required: true)]
        public List<EntitySpawnEntry> Items = new();

        /// <summary>
        ///     A sound to play when the items are spawned. For example, gift boxes being unwrapped.
        /// </summary>
        [DataField("sound", required: true)]
        public SoundSpecifier? Sound = null;

        /// <summary>
        ///     How many uses before the item should delete itself.
        /// </summary>
        /// <returns></returns>
        [DataField("uses")]
        public int Uses = 1;
    }
}
