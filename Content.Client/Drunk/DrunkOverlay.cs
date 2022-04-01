using Content.Shared.Drunk;
using Content.Shared.StatusEffect;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.Drunk
{
    public sealed class DrunkOverlay : Overlay
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;

        public override OverlaySpace Space => OverlaySpace.WorldSpace;
        public override bool RequestScreenTexture => true;
        private readonly ShaderInstance _drunkShader;

        private float _currentBoozePower = 0.0f;
        private float _nextBoozePower = 0.0f;

        private const float VisualThreshold = 10.0f;
        private const float PowerDivisor = 250.0f;

        public DrunkOverlay()
        {
            IoCManager.InjectDependencies(this);
            _drunkShader = _prototypeManager.Index<ShaderPrototype>("Drunk").InstanceUnique();
        }

        protected override void Draw(in OverlayDrawArgs args)
        {
            var playerEntity = _playerManager.LocalPlayer?.ControlledEntity;

            if (playerEntity == null)
                return;

            if (!_entityManager.HasComponent<DrunkComponent>(playerEntity)
                || !_entityManager.TryGetComponent<StatusEffectsComponent>(playerEntity, out var status))
                return;

            var statusSys = EntitySystem.Get<StatusEffectsSystem>();
            if (!statusSys.TryGetTime(playerEntity.Value, SharedDrunkSystem.DrunkKey, out var time, status))
                return;

            var left = (time.Value.Item2 - time.Value.Item1).TotalSeconds;

            _nextBoozePower = (float) left;
            _currentBoozePower += (_nextBoozePower - _currentBoozePower) / 1000.0f; // Approach the value smoothly so no lurching
            var visual = BoozePowerToVisual(_currentBoozePower);
            if (visual <= 0.0f)
                return;

            var handle = args.WorldHandle;
            var viewport = _eyeManager.GetWorldViewport();
            if (ScreenTexture != null)
                _drunkShader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
            _drunkShader.SetParameter("boozePower", Math.Clamp(visual, 0.0f, 1.0f));
            handle.UseShader(_drunkShader);
            handle.DrawRect(viewport, Color.White);
        }

        /// <summary>
        ///     Converts the # of seconds the drunk effect lasts for (booze power) to a percentage
        ///     used by the actual shader.
        /// </summary>
        /// <param name="boozePower"></param>
        private float BoozePowerToVisual(float boozePower)
        {
            return Math.Clamp((boozePower - VisualThreshold) / PowerDivisor, 0.0f, 1.0f);
        }
    }
}
