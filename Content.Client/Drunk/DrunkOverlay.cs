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
    public class DrunkOverlay : Overlay
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        public override OverlaySpace Space => OverlaySpace.WorldSpace;
        public override bool RequestScreenTexture => true;
        private readonly ShaderInstance _drunkShader;

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

            if (!playerEntity.HasComponent<DrunkComponent>()
                || !playerEntity.TryGetComponent<StatusEffectsComponent>(out var statusComp))
                return;

            var statusSys = EntitySystem.Get<StatusEffectsSystem>();
            if (!statusSys.TryGetTime(playerEntity.Uid, "Drunk", out var time))
                return;

            var left = (time.Value.Item2 - time.Value.Item1).TotalSeconds;

            var handle = args.WorldHandle;
            var viewport = _eyeManager.GetWorldViewport();
            if (ScreenTexture != null)
                _drunkShader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
            _drunkShader.SetParameter("boozePower", (float) left);
            _drunkShader.SetParameter("time", (float) _gameTiming.CurTime.TotalMilliseconds);
            handle.UseShader(_drunkShader);
            handle.DrawRect(viewport, Color.White);
        }
    }
}
