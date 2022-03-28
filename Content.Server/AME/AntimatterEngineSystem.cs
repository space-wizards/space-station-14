using Content.Server.AME.Components;
using Content.Server.Power.Components;
using JetBrains.Annotations;

namespace Content.Server.AME
{
    [UsedImplicitly]
    public sealed class AntimatterEngineSystem : EntitySystem
    {
        private float _accumulatedFrameTime;

        private const float UpdateCooldown = 10f;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<AMEControllerComponent, PowerChangedEvent>(OnAMEPowerChange);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            // TODO: Won't exactly work with replays I guess?
            _accumulatedFrameTime += frameTime;
            if (_accumulatedFrameTime >= UpdateCooldown)
            {
                foreach (var comp in EntityManager.EntityQuery<AMEControllerComponent>())
                {
                    comp.OnUpdate(frameTime);
                }
                _accumulatedFrameTime -= UpdateCooldown;
            }
        }

        private static void OnAMEPowerChange(EntityUid uid, AMEControllerComponent component, PowerChangedEvent args)
        {
            component.UpdateUserInterface();
        }
    }
}
