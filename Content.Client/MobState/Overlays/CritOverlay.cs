using Content.Shared.MobState.Components;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Client.MobState.Overlays
{
    public class CritOverlay : Overlay
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        public override OverlaySpace Space => OverlaySpace.WorldSpace;
        private readonly ShaderInstance _gradientCircleShader;

        public CritOverlay()
        {
            IoCManager.InjectDependencies(this);
            _gradientCircleShader = _prototypeManager.Index<ShaderPrototype>("GradientCircleMask").Instance();
        }

        public static bool LocalPlayerHasState(IPlayerManager pm, bool critical, bool dead) {
            var playerEntity = pm.LocalPlayer?.ControlledEntity;

            if (playerEntity == null)
            {
                return false;
            }

            if (playerEntity.TryGetComponent<MobStateComponent>(out var mobState))
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
            if (!LocalPlayerHasState(_playerManager, true, false))
                return;

            var worldHandle = args.WorldHandle;
            var viewport = _eyeManager.GetWorldViewport();
            worldHandle.UseShader(_gradientCircleShader);
            worldHandle.DrawRect(viewport, Color.White);
        }
    }
}
