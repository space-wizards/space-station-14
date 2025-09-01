using Content.Shared.Roles;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;
using Robust.Shared.GameStates;

namespace Content.Shared._Starlight.IdClothingBlocker;

[RegisterComponent, NetworkedComponent]
public sealed partial class IdClothingBlockerComponent : Component
{
    [DataField("isBlocked")]
    public bool IsBlocked = false;

    [DataField("allowedJobs", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<JobPrototype>))]
    public HashSet<string>? AllowedJobs = null;

    [DataField("beepSound")]
    public SoundSpecifier BeepSound = new SoundPathSpecifier("/Audio/Effects/beep1.ogg");
} 