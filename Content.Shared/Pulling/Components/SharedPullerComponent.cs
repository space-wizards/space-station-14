using Content.Shared.Pulling;
using Content.Shared.Movement.Components;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;
using Robust.Shared.Log;
using Component = Robust.Shared.GameObjects.Component;

namespace Content.Shared.Pulling.Components
{
    [RegisterComponent]
    [Friend(typeof(SharedPullingStateManagementSystem))]
    public class SharedPullerComponent : Component
    {
        public override string Name => "Puller";

        // Before changing how this is updated, please see SharedPullerSystem.RefreshMovementSpeed
        public float WalkSpeedModifier => Pulling == null ? 1.0f : 0.75f;

        public float SprintSpeedModifier => Pulling == null ? 1.0f : 0.75f;

        [ViewVariables]
        public IEntity? Pulling { get; set; }

        protected override void Shutdown()
        {
            EntitySystem.Get<SharedPullingStateManagementSystem>().ForceDisconnectPuller(this);
            base.Shutdown();
        }

        protected override void OnRemove()
        {
            if (Pulling != null)
            {
                // This is absolute paranoia but it's also absolutely necessary. Too many puller state bugs. - 20kdc
                Logger.ErrorS("c.go.c.pulling", "PULLING STATE CORRUPTION IMMINENT IN PULLER {0} - OnRemove called when Pulling is set!", Owner);
            }
            base.OnRemove();
        }
    }
}
