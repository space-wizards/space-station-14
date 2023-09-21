using Content.Shared.Access.Components;
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
        /// <summary>
        /// This gets set to ShredDuration every time this shreds something to track how much longer to shred.
        /// </summary>
        public float ShreddingTimeLeft = 0;

        /// <summary>
        ///     The container that shreds things.
        /// </summary>
        [DataField("container", required: true)]
        public string Container = default!;

        /// <summary>
        /// The sound that plays when this starts shredding something.
        /// </summary>
        [DataField("shreddingSound")]
        public SoundSpecifier ShreddingSound = new SoundPathSpecifier("/Audio/Machines/shredder.ogg");

        /// <summary>
        /// If present, this will only attempt to shred things on the whitelist.
        /// </summary>
        [DataField("whitelist")]
        public EntityWhitelist Whitelist = default!;

        /// <summary>
        /// Whether or not the shredder should shake while shredding.
        /// </summary>
        [DataField("doShake")]
        public bool DoShake = false;

        /// <summary>
        /// This gets set to ShredDuration every time this shreds something to track how much longer to shred.
        /// </summary>
        [DataField("shreddingTime")]
        public float ShreddingTime = 5;

        /// <summary>
        /// Whether or not the container should open up once this is done shredding.
        /// </summary>
        [DataField("openOnDone")]
        public bool OpenOnDone = true;

        /// <summary>
        /// Whether or not the container should be locked while it is active.
        /// </summary>
        [DataField("lockWhileShredding")]
        public bool LockWhileShredding = true;
    }

    public sealed class StartBeingShreddedEvent : HandledEntityEventArgs
    {
        public EntityUid Shredder;

        public StartBeingShreddedEvent(EntityUid shredder)
        {
            Shredder = shredder;
        }
    }

    public sealed class DoneBeingShreddedEvent : HandledEntityEventArgs
    {
        public EntityUid Shredder;

        public DoneBeingShreddedEvent(EntityUid shredder)
        {
            Shredder = shredder;
        }
    }
}
