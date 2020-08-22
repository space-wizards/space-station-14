using Robust.Shared.GameObjects;
using Content.Shared.GameObjects.Components.ActionBlocking;
using Robust.Client.Graphics;
using Robust.Client.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Client.GameObjects.Components.ActionBlocking
{
    [RegisterComponent]
    public class HandcuffComponent : SharedHandcuffComponent
    {
        private string _brokenName;
        private string _brokenDesc;

        public override void HandleComponentState(ComponentState curState, ComponentState nextState)
        {
            var cuffState = curState as HandcuffedComponentState;

            if (cuffState == null || cuffState.IconState == string.Empty)
            {
                return;
            }

            Owner.Name = _brokenName;
            Owner.Description = _brokenDesc;

            if (Owner.TryGetComponent<SpriteComponent>(out var sprite))
            {
                sprite.LayerSetState(0, new RSI.StateId(cuffState.IconState)); // TODO: safety check to see if RSI contains the state?
            }
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataFieldCached(ref _brokenName, "brokenName", default);
            serializer.DataFieldCached(ref _brokenDesc, "brokenDesc", default);
        }
    }
}
