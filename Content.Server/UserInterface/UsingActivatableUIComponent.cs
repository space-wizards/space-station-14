using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization;


namespace Content.Server.UserInterface
{
    [RegisterComponent]
    public sealed class UsingActivatableUIComponent : Component
    {
        public ActivatableUIComponent UI = default!;
    }
}

