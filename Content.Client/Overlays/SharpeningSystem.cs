using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Content.Shared.CCVar;
using Content.Client.Overlays;

namespace Content.Client.Overlays;

{
    public sealed class SharpeningSystem : EntitySystem
    {
        [Dependency] private readonly IOverlayManager _overlayManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;

        private SharpeningOverlay _overlay = default!;

        public override void Initialize()
        {
            base.Initialize();
            _overlay = new SharpeningOverlay();

            var initialSharpness = _cfg.GetCVar(CCVars.DisplaySharpening);
            OnSharpnessChanged(initialSharpness);

            _playerManager.LocalPlayerAttached += OnPlayerAttached;
            _playerManager.LocalPlayerDetached += OnPlayerDetached;

            _cfg.OnValueChanged(CCVars.DisplaySharpening, OnSharpnessChanged);
        }

        private void OnSharpnessChanged(int value)
        {
            if (_overlay != null)
                _overlay.Sharpness = value / 10f;
        }

        private void OnPlayerAttached(EntityUid entity)
        {
            _overlayManager.AddOverlay(_overlay);
        }

        private void OnPlayerDetached(EntityUid entity)
        {
            _overlayManager.RemoveOverlay(_overlay);
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _playerManager.LocalPlayerAttached -= OnPlayerAttached;
            _playerManager.LocalPlayerDetached -= OnPlayerDetached;
        }

        public void SetSharpness(float value)
        {
            if (_overlay != null)
                _overlay.Sharpness = value;
        }
    }
}
