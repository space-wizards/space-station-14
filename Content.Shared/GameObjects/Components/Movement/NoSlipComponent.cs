using Content.Shared.GameObjects.EntitySystems.EffectBlocker;
using Robust.Shared.GameObjects;

namespace Content.Shared.GameObjects.Components.Movement
{
    [RegisterComponent]
    public class NoSlipComponent : Component, IEffectBlocker
    {
        public override string Name => "NoSlip";

        bool IEffectBlocker.CanSlip() => false;
    }
}
