using Robust.Shared.Audio;
using Robust.Shared.Serialization;

namespace Content.Shared.Audio;


public abstract class SharedAdminSoundSystem : EntitySystem
{
}

[Serializable, NetSerializable]
public sealed class AdminSoundEvent : EntityEventArgs
{
    public string Filename;
    public AudioParams? AudioParams;
    public AdminSoundEvent(string filename, AudioParams? audioParams = null)
    {
        Filename = filename;
        AudioParams = audioParams;
    }
}
