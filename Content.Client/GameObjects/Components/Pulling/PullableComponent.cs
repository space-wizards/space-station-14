using Content.Shared.GameObjects.Components.Pulling;
using Robust.Shared.GameObjects;

namespace Content.Client.GameObjects.Components.Pulling
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedPullableComponent))]
    public class PullableComponent : SharedPullableComponent
    {
    }
}
