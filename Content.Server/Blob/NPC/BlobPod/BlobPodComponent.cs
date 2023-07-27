using Robust.Shared.Audio;

namespace Content.Server.Blob.NPC.BlobPod
{
    [RegisterComponent]
    public sealed class BlobPodComponent : Component
    {
        public bool IsDraining = false;
        /// <summary>
        /// The time (in seconds) that it takes to zombify an entity.
        /// </summary>
        [DataField("drainDelay")]
        public float DrainDelay = 5.00f;

        [DataField("drainSound")]
        public SoundSpecifier ZombifySoundPath = new SoundPathSpecifier("/Audio/Effects/clang2.ogg");

        [DataField("drainFinishSound")]
        public SoundSpecifier ZombifyFinishSoundPath = new SoundPathSpecifier("/Audio/Effects/guardian_inject.ogg");

        public IPlayingAudioStream? ZombifyStingStream;
        public EntityUid? ZombifyTarget;
    }
}


