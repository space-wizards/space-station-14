using Content.Shared.Pulling;
using Content.Shared.Movement.Components;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;
using Component = Robust.Shared.GameObjects.Component;

namespace Content.Shared.Pulling.Components
{
    [RegisterComponent]
    [Friend(typeof(SharedPullingStateManagementSystem))]
    public class SharedPullerComponent : Component, IMoveSpeedModifier
    {
        public override string Name => "Puller";

        public float WalkSpeedModifier => Pulling == null ? 1.0f : 0.75f;

        public float SprintSpeedModifier => Pulling == null ? 1.0f : 0.75f;

        [ViewVariables]
        public IEntity? Pulling { get; set; }

        protected override void OnRemove()
        {
            EntitySystem.Get<SharedPullingStateManagementSystem>().ForceDisconnectPuller(this);
            base.OnRemove();
        }
    }
}
