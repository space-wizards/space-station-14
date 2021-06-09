using Content.Shared.EffectBlocker;
using Robust.Shared.GameObjects;

namespace Content.Shared.Slippery
{
    [RegisterComponent]
    public class NoSlipComponent : Component, IEffectBlocker
    {
        public override string Name => "NoSlip";

        bool IEffectBlocker.CanSlip() => false;
    }
}
