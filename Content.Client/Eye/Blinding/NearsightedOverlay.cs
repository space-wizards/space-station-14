using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Content.Shared.Eye.Blinding;
using Content.Shared.Eye.Blinding.Components;

namespace Content.Client.Eye.Blinding
{
    public sealed class NearsightedOverlay : Overlay
    {
        [Dependency] private readonly IGameTiming _timing = default!;
		[Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;


        public override OverlaySpace Space => OverlaySpace.WorldSpace;
        private readonly ShaderInstance _nearsightShader;

        public float OxygenLevel = 0f;
		private float _oldOxygenLevel = 0f;
		public float outerDarkness = 1f;

        public NearsightedOverlay()
        {
            IoCManager.InjectDependencies(this);
            _nearsightShader = _prototypeManager.Index<ShaderPrototype>("GradientCircleMask").InstanceUnique();
        }

        protected override void Draw(in OverlayDrawArgs args)
        {
            if (_playerManager.LocalPlayer?.ControlledEntity is not {Valid: true} player)
                return;
			if (!_entityManager.HasComponent<NearsightedComponent>(player))
                return;
			if (!_entityManager.TryGetComponent(player, out EyeComponent? eyeComp))
				return;
			if (args.Viewport.Eye != eyeComp.Eye)
				return;

            var viewport = args.WorldAABB;
			var handle = args.WorldHandle;
			var distance = args.ViewportBounds.Width;

			var time = (float) _timing.RealTime.TotalSeconds;
			var lastFrameTime = (float) _timing.FrameTime.TotalSeconds;

			if (!MathHelper.CloseTo(_oldOxygenLevel, OxygenLevel, 0.001f))
			{
				var diff = OxygenLevel - _oldOxygenLevel;
				_oldOxygenLevel += GetDiff(diff, lastFrameTime);
			}
			else
			{
				_oldOxygenLevel = OxygenLevel;
			}

			/*
			 * darknessAlphaOuter is the maximum alpha for anything outside of the larger circle
			 * darknessAlphaInner (on the shader) is the alpha for anything inside the smallest circle
			 *
			 * outerCircleRadius is what we end at for max level for the outer circle
			 * outerCircleMaxRadius is what we start at for 0 level for the outer circle
			 *
			 * innerCircleRadius is what we end at for max level for the inner circle
			 * innerCircleMaxRadius is what we start at for 0 level for the inner circle
			 */

			float outerMaxLevel = 0.6f * distance;
			float outerMinLevel = 0.06f * distance;
			float innerMaxLevel = 0.02f * distance;
			float innerMinLevel = 0.02f * distance;

			var outerRadius = outerMaxLevel - OxygenLevel * (outerMaxLevel - outerMinLevel);
			var innerRadius = innerMaxLevel - OxygenLevel * (innerMaxLevel - innerMinLevel);

			_nearsightShader.SetParameter("time", 0.0f);
			_nearsightShader.SetParameter("color", new Vector3(1f, 1f, 1f));
			_nearsightShader.SetParameter("darknessAlphaOuter", outerDarkness);
			_nearsightShader.SetParameter("innerCircleRadius", innerRadius);
			_nearsightShader.SetParameter("innerCircleMaxRadius", innerRadius);
			_nearsightShader.SetParameter("outerCircleRadius", outerRadius);
			_nearsightShader.SetParameter("outerCircleMaxRadius", outerRadius + 0.2f * distance);
			handle.UseShader(_nearsightShader);
			handle.DrawRect(viewport, Color.Black);

			handle.UseShader(null);
        }
		
		private float GetDiff(float value, float lastFrameTime)
		{
			var adjustment = value * 5f * lastFrameTime;

			if (value < 0f)
				adjustment = Math.Clamp(adjustment, value, -value);
			else
				adjustment = Math.Clamp(adjustment, -value, value);

			return adjustment;
		}
    }
}
