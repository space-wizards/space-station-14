using Content.Shared.GameObjects.Components.Movement;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

#nullable enable

namespace Content.Client.GameObjects.Components.Movement
{
    [RegisterComponent]
    [ComponentReference(typeof(IMoverComponent))]
    public class PlayerInputMoverComponent : SharedPlayerInputMoverComponent, IMoverComponent
    {
        public override GridCoordinates LastPosition { get; set; }
        public override float StepSoundDistance { get; set; }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            if (IoCManager.Resolve<IPlayerManager>().LocalPlayer!.ControlledEntity == Owner)
            {
                base.HandleComponentState(curState, nextState);
            }
        }
    }
}
