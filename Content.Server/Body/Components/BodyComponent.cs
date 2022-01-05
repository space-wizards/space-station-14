using Content.Server.Body.Systems;
using Content.Shared.Body.Components;
using Content.Shared.Sound;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Body.Components;

[RegisterComponent]
[ComponentReference(typeof(SharedBodyComponent))]
[Friend(typeof(BodySystem))]
public class BodyComponent : SharedBodyComponent
{
    [DataField("gibSound")] public SoundSpecifier GibSound = new SoundCollectionSpecifier("gib");
}
