using Content.Shared.Clothing;
using Robust.Shared.GameObjects;

namespace Content.Client.Cloning
{
    [RegisterComponent]
    public sealed class MagbootsComponent : SharedMagbootsComponent
    {
        public override bool On { get; set; }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            if (curState is not MagbootsComponentState compState)
                return;

            On = compState.On;
            OnChanged();
        }
    }
}
