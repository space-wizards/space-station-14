using Content.Shared.Polymorph;
using Content.Shared.Sound;
using Content.Shared.Whitelist;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Polymorph.Components;

[RegisterComponent]
public sealed class PolymorphOnCollideComponent : Component
{
    [DataField("polymorph", required: true, customTypeSerializer:typeof(PrototypeIdSerializer<PolymorphPrototype>))]
    public string Polymorph = default!;

    [DataField("whitelist", required: true)]
    public EntityWhitelist Whitelist = default!;

    [DataField("blacklist")]
    public EntityWhitelist? Blacklist;

    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Magic/forcewall.ogg");
}
