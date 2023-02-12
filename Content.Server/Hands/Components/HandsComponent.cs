using Content.Shared.Hands.Components;

namespace Content.Server.Hands.Components;

[RegisterComponent]
[ComponentReference(typeof(SharedHandsComponent))]
public sealed class HandsComponent : SharedHandsComponent
{
}
