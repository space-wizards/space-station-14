using Robust.Shared.Audio;

namespace Content.Server.Blob.NPC.BlobPod
{
    [RegisterComponent]
    public sealed class BlobPodComponent : Component
    {
        public bool IsZombifying = false;

        public EntityUid? ZombifiedEntityUid = default!;

        /// <summary>
        /// The time (in seconds) that it takes to zombify an entity.
        /// </summary>
        [DataField("zombifyDelay")]
        public float ZombifyDelay = 5.00f;

        [DataField("zombifySoundPath")]
        public SoundSpecifier ZombifySoundPath = new SoundPathSpecifier("/Audio/Effects/Fluids/blood1.ogg");

        [DataField("zombifyFinishSoundPath")]
        public SoundSpecifier ZombifyFinishSoundPath = new SoundPathSpecifier("/Audio/Effects/gib1.ogg");

        public IPlayingAudioStream? ZombifyStingStream;
        public EntityUid? ZombifyTarget;
    }
}


