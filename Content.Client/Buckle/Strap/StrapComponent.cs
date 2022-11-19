using Content.Shared.Buckle.Components;

namespace Content.Client.Buckle.Strap;

[RegisterComponent]
[ComponentReference(typeof(SharedStrapComponent))]
[Access(typeof(BuckleSystem))]
public sealed class StrapComponent : SharedStrapComponent
{
}
