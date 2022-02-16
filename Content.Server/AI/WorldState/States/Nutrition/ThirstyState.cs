using System;
using Content.Server.Nutrition.Components;
using Content.Shared.Nutrition.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.AI.WorldState.States.Nutrition
{
    [UsedImplicitly]
    public sealed class ThirstyState : StateData<bool>
    {
        public override string Name => "Thirsty";

        public override bool GetValue()
        {
            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(Owner, out ThirstComponent? thirstComponent))
            {
                return false;
            }

            switch (thirstComponent.CurrentThirstThreshold)
            {
                case ThirstThreshold.OverHydrated:
                    return false;
                case ThirstThreshold.Okay:
                    return false;
                case ThirstThreshold.Thirsty:
                    return true;
                case ThirstThreshold.Parched:
                    return true;
                case ThirstThreshold.Dead:
                    return true;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(thirstComponent.CurrentThirstThreshold),
                        thirstComponent.CurrentThirstThreshold,
                        null);
            }
        }
    }
}
