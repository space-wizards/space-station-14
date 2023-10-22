using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Content.Shared.Eye.Blinding;
using Content.Shared.Eye.Blinding.Components;

namespace Content.Client.Eye.Blinding
{
    public sealed class MonochromacyOverlay : Overlay
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] IEntityManager _entityManager = default!;


        public override bool RequestScreenTexture => true;
        public override OverlaySpace Space => OverlaySpace.WorldSpace;
        private readonly ShaderInstance _greyscaleShader;
		
		public float AlphaValue = 1.0f;

        private MonochromacyComponent _monochromacyComponent = default!;

        public MonochromacyOverlay()
        {
            IoCManager.InjectDependencies(this);
            _greyscaleShader = _prototypeManager.Index<ShaderPrototype>("GreyscaleFullscreen").InstanceUnique();
        }

        protected override void Draw(in OverlayDrawArgs args)
        {
            if (ScreenTexture == null)
                return;
            if (_playerManager.LocalPlayer?.ControlledEntity is not {Valid: true} player)
                return;
			if (!_entityManager.HasComponent<MonochromacyComponent>(player))
                return;
			if (!_entityManager.TryGetComponent(player, out EyeComponent? eyeComp))
				return;
			if (args.Viewport.Eye != eyeComp.Eye)
				return;

            _greyscaleShader?.SetParameter("SCREEN_TEXTURE", ScreenTexture);
			_greyscaleShader?.SetParameter("alpha", AlphaValue);

            var worldHandle = args.WorldHandle;
            var viewport = args.WorldBounds;
            worldHandle.SetTransform(Matrix3.Identity);
            worldHandle.UseShader(_greyscaleShader);
            worldHandle.DrawRect(viewport, Color.White);
            worldHandle.UseShader(null);
        }
    }
}
