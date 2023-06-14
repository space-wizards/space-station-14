using Content.Shared.Traits.Assorted;
using Robust.Shared.Audio;

namespace Content.Server.Traits.Assorted;

public sealed class ParacusiaSystem : SharedParacusiaSystem
{
    public void SetSounds(EntityUid uid, SoundSpecifier sounds, ParacusiaComponent? component = null)
    {
        if (!Resolve(uid, ref component))
        {
            return;
        }
        component.Sounds = sounds;
        Dirty(component);
    }
}
