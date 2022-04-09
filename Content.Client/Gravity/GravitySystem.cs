using Content.Shared.Gravity;
using Robust.Shared.GameStates;

namespace Content.Client.Gravity
{
    internal sealed class GravitySystem : SharedGravitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GravityComponent, ComponentHandleState>(OnHandleState);
        }

        private void OnHandleState(EntityUid uid, GravityComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not GravityComponent.GravityComponentState state)
                return;

            if (component.Enabled == state.Enabled)
                return;

            component.Enabled = state.Enabled;

            RaiseLocalEvent(new GravityChangedMessage(Transform(uid).GridID, component.Enabled));
        }
    }
}
