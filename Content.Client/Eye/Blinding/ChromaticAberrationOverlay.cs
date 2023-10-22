using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Content.Shared.Eye.Blinding;
using Content.Shared.Eye.Blinding.Components;

namespace Content.Client.Eye.Blinding
{
    public sealed class ChromaticAberrationOverlay : Overlay
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] IEntityManager _entityManager = default!;


        public override bool RequestScreenTexture => true;
        public override OverlaySpace Space => OverlaySpace.WorldSpace;
        private readonly ShaderInstance _chromaticAberrationShader;
		
		public float AlphaValue = 1.0f;
		
		public float[][] Matr = new float[][] {
			new float[] {0.625f, 0.375f, 0f},
			new float[] {0.7f, 0.3f, 0f},
			new float[] {0f, 0.3f, 0.7f}};

        private ChromaticAberrationComponent _ChromaticAberrationComponent = default!;

        public ChromaticAberrationOverlay()
        {
            IoCManager.InjectDependencies(this);
            _chromaticAberrationShader = _prototypeManager.Index<ShaderPrototype>("ChromaticAberration").InstanceUnique();
        }

        protected override void Draw(in OverlayDrawArgs args)
        {
            if (ScreenTexture == null)
                return;
            if (_playerManager.LocalPlayer?.ControlledEntity is not {Valid: true} player)
                return;
			if (!_entityManager.HasComponent<ChromaticAberrationComponent>(player))
                return;
			if (!_entityManager.TryGetComponent(player, out EyeComponent? eyeComp))
				return;
			if (args.Viewport.Eye != eyeComp.Eye)
				return;

            _chromaticAberrationShader?.SetParameter("SCREEN_TEXTURE", ScreenTexture);
			_chromaticAberrationShader?.SetParameter("alpha", AlphaValue);
			
			_chromaticAberrationShader?.SetParameter("a1", Matr[0][0]);
			_chromaticAberrationShader?.SetParameter("a2", Matr[0][1]);
			_chromaticAberrationShader?.SetParameter("a3", Matr[0][2]);
			_chromaticAberrationShader?.SetParameter("b1", Matr[1][0]);
			_chromaticAberrationShader?.SetParameter("b2", Matr[1][1]);
			_chromaticAberrationShader?.SetParameter("b3", Matr[1][2]);
			_chromaticAberrationShader?.SetParameter("c1", Matr[2][0]);
			_chromaticAberrationShader?.SetParameter("c2", Matr[2][1]);
			_chromaticAberrationShader?.SetParameter("c3", Matr[2][2]);

            var worldHandle = args.WorldHandle;
            var viewport = args.WorldBounds;
            worldHandle.SetTransform(Matrix3.Identity);
            worldHandle.UseShader(_chromaticAberrationShader);
            worldHandle.DrawRect(viewport, Color.White);
            worldHandle.UseShader(null);
        }
    }
}
