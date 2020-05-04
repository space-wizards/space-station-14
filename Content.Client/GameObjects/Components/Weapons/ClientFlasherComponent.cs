using System.Threading;
using Content.Shared.GameObjects.Components.Weapons;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;
using Timer = Robust.Shared.Timers.Timer;

namespace Content.Client.GameObjects.Components.Weapons
{
    [RegisterComponent]
    [UsedImplicitly]
    public class ClientFlasherComponent : SharedFlasherComponent
    {
        // Controls for the flash's brief light
        private CancellationTokenSource _lightCancellationTokenSource;
        private PointLightComponent _pointLight;
        private float _lightDuration;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _lightDuration, "light_duration", 0.5f);
        }

        public override void HandleMessage(ComponentMessage message, IComponent component)
        {
            base.HandleMessage(message, component);
            switch (message)
            {
                case FlasherComponentMessage _:
                    PlayAnimation();
                    break;
            }
        }

        // TODO: Refactor this out into a BriefLight component

        private void PlayAnimation()
        {
            //Play anim on the icon

            // Add light
            if (_pointLight == null)
            {
                _pointLight = Owner.AddComponent<PointLightComponent>();
                _lightCancellationTokenSource = new CancellationTokenSource();
            }

            _lightCancellationTokenSource?.Cancel();

            Timer.Spawn((int) _lightDuration * 1000, () =>
            {
                _lightCancellationTokenSource = null;
                Owner.RemoveComponent<PointLightComponent>();
            }, _lightCancellationTokenSource.Token);
        }

        public void OnUpdate(float frameTime)
        {
            // For the flash light
        }
    }
}
