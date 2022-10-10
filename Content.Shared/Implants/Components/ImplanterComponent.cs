using System.Threading;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Containers;

namespace Content.Shared.Implants.Components;
[RegisterComponent]
public sealed class ImplanterComponent : Component
{
    //TODO: See if you need to add anything else to the implanter
    //Things like inject only, draw, unremoveable, etc.

    public CancellationTokenSource? CancelToken;
}
