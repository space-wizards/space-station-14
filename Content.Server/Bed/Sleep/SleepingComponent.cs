using Robust.Shared.GameStates;
using Content.Shared.Bed.Sleep;

namespace Content.Server.Bed.Sleep
{
    [NetworkedComponent, RegisterComponent]
    public sealed class SleepingComponent : SharedSleepingComponent
    {}
}
