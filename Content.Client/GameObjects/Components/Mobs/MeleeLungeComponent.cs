using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Content.Client.GameObjects.Components.Mobs
{
    [RegisterComponent]
    public sealed class MeleeLungeComponent : Component
    {
        public override string Name => "MeleeLunge";

        private const float ResetTime = 0.3f;
        private const float BaseOffset = 0.25f;

        private Angle _angle;
        private float _time;

        public void SetData(Angle angle)
        {
            _angle = angle;
            _time = 0;
        }

        public void Update(float frameTime)
        {
            _time += frameTime;

            var offset = Vector2.Zero;
            var deleteSelf = false;

            if (_time > ResetTime)
            {
                deleteSelf = true;
            }
            else
            {
                offset = _angle.RotateVec((BaseOffset, 0));
                offset *= (ResetTime - _time) / ResetTime;
            }

            if (Owner.TryGetComponent(out ISpriteComponent spriteComponent))
            {
                // We have to account for rotation so the offset still checks out.
                // SpriteComponent.Offset is applied before transform rotation (as expected).
                var worldRotation = Owner.Transform.WorldRotation;
                spriteComponent.Offset = new Angle(-worldRotation).RotateVec(offset);
            }

            if (deleteSelf)
            {
                Owner.RemoveComponent<MeleeLungeComponent>();
            }
        }
    }
}
