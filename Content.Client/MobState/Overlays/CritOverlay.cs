using Content.Shared.MobState.Components;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Client.MobState.Overlays
{
    public sealed class CritOverlay : Overlay
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IEntityManager _entities = default!;

        public override OverlaySpace Space => OverlaySpace.WorldSpace;
        private readonly ShaderInstance _gradientCircleShader;

        public CritOverlay()
        {
            IoCManager.InjectDependencies(this);
            _gradientCircleShader = _prototypeManager.Index<ShaderPrototype>("GradientCircleMask").Instance();
        }

        public static bool LocalPlayerHasState(IPlayerManager pm, bool critical, bool dead, IEntityManager? entities = null) {
            if (pm.LocalPlayer?.ControlledEntity is not {Valid: true} player)
            {
                return false;
            }

            IoCManager.Resolve(ref entities);
            if (entities.TryGetComponent<MobStateComponent?>(player, out var mobState))
            {
                if (critical)
                    if (mobState.IsCritical())
                        return true;
                if (dead)
                    if (mobState.IsDead())
                        return true;
            }

            return false;
        }

        protected override void Draw(in OverlayDrawArgs args)
        {
            if (!LocalPlayerHasState(_playerManager, true, false, _entities))
                return;

            var worldHandle = args.WorldHandle;
            var viewport = args.WorldAABB;
            worldHandle.UseShader(_gradientCircleShader);
            worldHandle.DrawRect(viewport, Color.White);
        }
    }
}
