#nullable enable
using Content.Shared.GameObjects.Components.Strap;
using Robust.Shared.GameObjects;

namespace Content.Client.GameObjects.Components.Strap
{
    [RegisterComponent]
    public class StrapComponent : SharedStrapComponent
    {
        public override StrapPosition Position { get; protected set; }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            if (!(curState is StrapComponentState strap))
            {
                return;
            }

            Position = strap.Position;
        }
    }
}
