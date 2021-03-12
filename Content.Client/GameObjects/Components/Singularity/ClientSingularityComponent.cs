using Content.Shared.GameObjects.Components.Singularity;
using Robust.Shared.GameObjects;

namespace Content.Client.GameObjects.Components.Singularity
{
    [RegisterComponent]
    [ComponentReference(typeof(IClientSingularityInstance))]
    class ClientSingularityComponent : SharedSingularityComponent, IClientSingularityInstance
    {
        public int Level
        {
            get
            {
                return _level;
            }
            set
            {
                _level = value;
            }
        }
        private int _level;

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            if (curState is not SingularityComponentState state)
            {
                return;
            }
            _level = state.Level;
        }
    }
}
