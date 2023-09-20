using Content.Shared.Whitelist;
using Robust.Shared.Audio;

namespace Content.Server.Containers
{
    /// <summary>
    ///     A shredder that gibs bodies when they are inserted into a container on the same entity
    /// </summary>
    [RegisterComponent]
    [Access(typeof(ShredderSystem))]
    public sealed partial class ShredderComponent : Component
    {
        [DataField("shreddingSound")]
        public SoundSpecifier ShreddingSound = new SoundPathSpecifier("/Audio/Machines/shredder.ogg");

        [DataField("whitelist", required: true)]
        public EntityWhitelist Whitelist = default!;
    }

    public sealed class BeingShreddedEvent : HandledEntityEventArgs
    {
        public EntityUid Shredder;

        public BeingShreddedEvent(EntityUid shredder)
        {
            Shredder = shredder;
        }
    }
}
