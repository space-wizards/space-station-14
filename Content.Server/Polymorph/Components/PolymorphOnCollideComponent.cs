using Content.Server.Polymorph.Systems;
using Content.Shared.Polymorph;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.Polymorph.Components;

[RegisterComponent]
[Access(typeof(PolymorphSystem))]
public sealed partial class PolymorphOnCollideComponent : Component
{
    [DataField(required: true)]
    public ProtoId<PolymorphPrototype> Polymorph;

    [DataField(required: true)]
    public EntityWhitelist Whitelist = default!;

    [DataField]
    public EntityWhitelist? Blacklist;

    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Magic/forcewall.ogg");
}
