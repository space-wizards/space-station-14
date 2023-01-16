using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Nutrition.Components;
using Robust.Shared.GameObjects;

namespace Content.Client.Nutrition.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedHungerComponent))]
    public sealed class HungerComponent : SharedHungerComponent
    {
    }
}
