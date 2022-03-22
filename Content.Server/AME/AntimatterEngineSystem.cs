using Content.Server.AME.Components;
using Content.Server.Power.Components;
using JetBrains.Annotations;

namespace Content.Server.AME
{
    [UsedImplicitly]
    public sealed class AntimatterEngineSystem : EntitySystem
    {
        private float _accumulatedFrameTime;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<AMEControllerComponent, PowerChangedEvent>(OnAMEPowerChange);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            _accumulatedFrameTime += frameTime;
            if (_accumulatedFrameTime >= 10)
            {
                foreach (var comp in EntityManager.EntityQuery<AMEControllerComponent>())
                {
                    comp.OnUpdate(frameTime);
                }
                _accumulatedFrameTime -= 10;
            }

        }

        private static void OnAMEPowerChange(EntityUid uid, AMEControllerComponent component, ref PowerChangedEvent args)
        {
            component.UpdateUserInterface();
        }
    }
}
