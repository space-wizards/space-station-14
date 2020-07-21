using Content.Shared.GameObjects.Components.Pointing;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using DrawDepth = Content.Shared.GameObjects.DrawDepth;

namespace Content.Server.GameObjects.Components.Pointing
{
    [RegisterComponent]
    public class PointingArrowComponent : SharedPointingArrowComponent
    {
        private const float Step = 1;

        private float _duration;
        private float _currentStep;
        private bool _up;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _duration, "duration", 4);
        }

        protected override void Startup()
        {
            base.Startup();

            if (Owner.TryGetComponent(out SpriteComponent sprite))
            {
                sprite.DrawDepth = (int) DrawDepth.Overlays;
            }
        }

        public void Update(float frameTime)
        {
            Owner.Transform.LocalPosition += (0, _up ? 0.01f : -0.01f);

            _duration -= frameTime;
            _currentStep -= frameTime;

            if (_duration <= 0)
            {
                Owner.Delete();
                return;
            }

            if (_currentStep <= 0)
            {
                _currentStep = Step;
                _up ^= true;
            }
        }
    }
}
