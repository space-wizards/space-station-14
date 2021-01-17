using Content.Shared.GameObjects.Components;
using Robust.Shared.GameObjects;

#nullable enable

namespace Content.Client.GameObjects.Components
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
