using System.Threading;
using Robust.Shared.Containers;

namespace Content.Server.Mech.Equipment.Components;

[RegisterComponent]
public sealed class MechGrabberComponent : Component
{
    [DataField("energyPerGrab")]
    public float EnergyPerGrab = -20;

    [DataField("grabDelay")]
    public float GrabDelay = 3;

    [DataField("depositOffset")]
    public Vector2 DepositOffset = new(0, -1);

    [DataField("maxContents")]
    public int MaxContents = 15;

    public Container ItemContainer = default!;

    public CancellationTokenSource? Token;
}
