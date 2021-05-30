using Content.Shared.GameObjects.Components.Singularity;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Client.GameObjects.Components.Singularity
{
    [RegisterComponent]
    [ComponentReference(typeof(IClientSingularityInstance))]
    class ClientSingularityComponent : SharedSingularityComponent, IClientSingularityInstance
    {
        [ViewVariables]
        public int Level { get; set; }

        //I am lazy
        [ViewVariables]
        public float Intensity
        {
            get
            {
                switch (Level)
                {
                    case 0:
                        return 0.0f;
                    case 1:
                        return 2.7f;
                    case 2:
                        return 14.4f;
                    case 3:
                        return 47.2f;
                    case 4:
                        return 180.0f;
                    case 5:
                        return 600.0f;
                    case 6:
                        return 800.0f;
                }
                return -1.0f;
            }
        }
        [ViewVariables]
        public float Falloff
        {
            get
            {
                switch (Level)
                {
                    case 0:
                        return 9999f;
                    case 1:
                        return 6.4f;
                    case 2:
                        return 7.0f;
                    case 3:
                        return 8.0f;
                    case 4:
                        return 10.0f;
                    case 5:
                        return 12.0f;
                    case 6:
                        return 12.0f;
                }
                return -1.0f;
            }
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            if (curState is not SingularityComponentState state)
            {
                return;
            }
            Level = state.Level;
        }
    }
}
