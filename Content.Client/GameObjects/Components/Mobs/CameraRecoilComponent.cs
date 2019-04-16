using System;
using Content.Shared.GameObjects.Components.Mobs;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Maths;

namespace Content.Client.GameObjects.Components.Mobs
{
    public sealed class CameraRecoilComponent : SharedCameraRecoilComponent
    {
        // Maximum rate of magnitude restore towards 0 kick.
        private const float RestoreRateMax = 1.5f;

        // Minimum rate of magnitude restore towards 0 kick.
        private const float RestoreRateMin = 0.5f;

        // Time in seconds since the last kick that lerps RestoreRateMin and RestoreRateMax
        private const float RestoreRateRamp = 0.05f;

        // The maximum magnitude of the kick applied to the camera at any point.
        private const float KickMagnitudeMax = 0.25f;

        private Vector2 _currentKick;
        private float _lastKickTime;

        private EyeComponent _eye;

        public override void Initialize()
        {
            base.Initialize();

            _eye = Owner.GetComponent<EyeComponent>();
        }

        public override void Kick(Vector2 recoil)
        {
            // Use really bad math to "dampen" kicks when we're already kicked.
            var existing = _currentKick.Length;
            var dampen = existing/KickMagnitudeMax;
            _currentKick += recoil * (1-dampen);
            if (_currentKick.Length > KickMagnitudeMax)
            {
                _currentKick = _currentKick.Normalized * KickMagnitudeMax;
            }

            _lastKickTime = 0;
            _updateEye();
        }

        public override void HandleMessage(ComponentMessage message, INetChannel netChannel = null, IComponent component = null)
        {
            base.HandleMessage(message, netChannel, component);

            switch (message)
            {
                case RecoilKickMessage msg:
                    Kick(msg.Recoil);
                    break;
            }
        }

        public void FrameUpdate(float frameTime)
        {
            var magnitude = _currentKick.Length;
            if (magnitude <= 0.005f)
            {
                _currentKick = Vector2.Zero;
                _updateEye();
                return;
            }

            // Continually restore camera to 0.
            var normalized = _currentKick.Normalized;
            var restoreRate = FloatMath.Lerp(RestoreRateMin, RestoreRateMax, Math.Min(1, _lastKickTime/RestoreRateRamp));
            var restore = normalized * restoreRate * frameTime;
            _currentKick -= restore;
            _updateEye();
        }

        private void _updateEye()
        {
            _eye.Offset = _currentKick;
        }
    }
}
