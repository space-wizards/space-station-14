using Content.Server.Body.Systems;
using Content.Shared.Body.Components;
using Content.Shared.Sound;

namespace Content.Server.Body.Components;

[RegisterComponent]
[ComponentReference(typeof(SharedBodyComponent))]
[Access(typeof(BodySystem))]
public sealed class BodyComponent : SharedBodyComponent
{
    [DataField("gibSound")] public SoundSpecifier GibSound = new SoundCollectionSpecifier("gib");
}
