using Content.Shared.Audio;

namespace Content.Client.Audio;

public sealed partial class ContentAudioSystem : SharedContentAudioSystem
{
    public override void Initialize()
    {
        base.Initialize();
        InitializeAmbientMusic();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        ShutdownAmbientMusic();
    }
}
