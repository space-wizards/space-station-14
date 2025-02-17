using Content.Shared.NPC.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Server.Starlight.FactionClothingBlockerSystem;

[RegisterComponent]
public sealed partial class AccessClothingBlockerComponent : Component
{
    [DataField("access", required: false)]
    public string? Access = null;

    [DataField("beepSound")]
    public SoundSpecifier BeepSound = new SoundPathSpecifier("/Audio/Effects/beep1.ogg");
}
