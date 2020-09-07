using Content.Shared.GameObjects.Components.Pulling;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Shared.GameObjects.Components.Items
{
    public interface ISharedHandsComponent : IComponent
    {
        bool StartPull(SharedPullableComponent pullable);

        bool StopPull();

        bool TogglePull(SharedPullableComponent pullable);
    }
}
