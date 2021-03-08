using Content.Shared.GameObjects.Components.Mobs.State;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Client.Graphics.Overlays
{
    public class GradientCircleMaskOverlay : Overlay
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        public override OverlaySpace Space => OverlaySpace.WorldSpace;
        private readonly ShaderInstance _shader;

        public GradientCircleMaskOverlay() : base(nameof(GradientCircleMaskOverlay))
        {
            IoCManager.InjectDependencies(this);
            _shader = _prototypeManager.Index<ShaderPrototype>("GradientCircleMask").Instance();
        }

        public static bool LocalPlayerHasState(IPlayerManager pm, bool critical, bool dead) {
            var playerEntity = pm.LocalPlayer?.ControlledEntity;

            if (playerEntity == null)
            {
                return false;
            }

            if (playerEntity.TryGetComponent<IMobStateComponent>(out var mobState))
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

        protected override void Draw(DrawingHandleBase handle, OverlaySpace currentSpace)
        {
            if (!LocalPlayerHasState(_playerManager, true, false))
                return;

            handle.UseShader(_shader);
            var worldHandle = (DrawingHandleWorld)handle;
            var viewport = _eyeManager.GetWorldViewport();
            worldHandle.DrawRect(viewport, Color.White);
        }
    }
}
