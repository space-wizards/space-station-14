using Robust.Shared.Audio;

namespace Content.Server.Blob.NPC.BlobPod
{
    [RegisterComponent]
    public sealed class BlobPodComponent : Component
    {
        [ViewVariables(VVAccess.ReadOnly)]
        public bool IsZombifying = false;

        [ViewVariables(VVAccess.ReadOnly)]
        public EntityUid? ZombifiedEntityUid = default!;

        [ViewVariables(VVAccess.ReadWrite), DataField("zombifyDelay")]
        public float ZombifyDelay = 5.00f;

        [ViewVariables(VVAccess.ReadOnly)]
        public EntityUid? Core = null;

        [ViewVariables(VVAccess.ReadWrite), DataField("zombifySoundPath")]
        public SoundSpecifier ZombifySoundPath = new SoundPathSpecifier("/Audio/Effects/Fluids/blood1.ogg");

        [ViewVariables(VVAccess.ReadWrite), DataField("zombifyFinishSoundPath")]
        public SoundSpecifier ZombifyFinishSoundPath = new SoundPathSpecifier("/Audio/Effects/gib1.ogg");

        public IPlayingAudioStream? ZombifyStingStream;
        public EntityUid? ZombifyTarget;
    }
}


