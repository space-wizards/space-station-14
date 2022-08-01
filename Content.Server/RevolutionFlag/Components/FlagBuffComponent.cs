using Robust.Shared.GameStates;

namespace Content.Server.RevolutionFlag.Components
{
    [Access(typeof(FlagSystem))]
    [RegisterComponent, NetworkedComponent]
    public sealed class FlagBuffComponent : Component
    {
    }
}