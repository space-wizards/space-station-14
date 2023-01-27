using Content.Shared.Hands.Components;

namespace Content.Server.Hands.Components;

[RegisterComponent]
[ComponentReference(typeof(SharedHandsComponent))]
public sealed partial class HandsComponent : SharedHandsComponent
{
}
