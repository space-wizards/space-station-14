using Content.Shared.Body.Part;
using Robust.Shared.Audio;
using Robust.Shared.Serialization;

namespace Content.Shared.Body.Components;

[Serializable, NetSerializable]
public sealed class BodyComponentState : ComponentState
{
    public readonly BodyPartSlot? Root;
    public readonly SoundSpecifier GibSound;

    public BodyComponentState(BodyPartSlot? root, SoundSpecifier gibSound)
    {
        Root = root;
        GibSound = gibSound;
    }
}
