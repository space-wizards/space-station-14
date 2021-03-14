using Content.Shared.GameObjects.Components.Projectiles;
using Robust.Shared.GameObjects;

namespace Content.Client.GameObjects.Components.Projectiles
{
    [RegisterComponent]
    public class ProjectileComponent : SharedProjectileComponent
    {
        protected override EntityUid Shooter => _shooter;
        private EntityUid _shooter;

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            if (curState is ProjectileComponentState compState)
            {
                _shooter = compState.Shooter;
                IgnoreShooter = compState.IgnoreShooter;
            }
        }
    }
}
