using Content.Shared.Pulling.Components;
using Robust.Shared.GameObjects;

namespace Content.Client.Pulling
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedPullableComponent))]
    public class PullableComponent : SharedPullableComponent
    {
    }
}
