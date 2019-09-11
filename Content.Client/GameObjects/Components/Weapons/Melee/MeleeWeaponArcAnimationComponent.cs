using Content.Shared.GameObjects.Components.Weapons.Melee;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Content.Client.GameObjects.Components.Weapons.Melee
{
    [RegisterComponent]
    public sealed class MeleeWeaponArcAnimationComponent : Component
    {
        public override string Name => "MeleeWeaponArcAnimation";

        private WeaponArcPrototype _weaponArc;

        private float _timer;
        private SpriteComponent _sprite;

        public override void Initialize()
        {
            base.Initialize();

            _sprite = Owner.GetComponent<SpriteComponent>();
        }

        public void SetData(WeaponArcPrototype prototype)
        {
            _weaponArc = prototype;
            _sprite.AddLayer(new RSI.StateId(prototype.State));
        }

        internal void Update(float frameTime)
        {
            if (_weaponArc == null)
            {
                return;
            }

            _timer += frameTime;

            var (r, g, b, a) =
                Vector4.Clamp(_weaponArc.Color + _weaponArc.ColorDelta * _timer, Vector4.Zero, Vector4.One);
            _sprite.Color = new Color(r, g, b, a);

            switch (_weaponArc.ArcType)
            {
                case WeaponArcType.Slash:
                    var angle = Angle.FromDegrees(_weaponArc.Width)/2;
                    _sprite.Rotation = Angle.Lerp(-angle, angle, (float) (_timer / _weaponArc.Length.TotalSeconds));
                    break;

                case WeaponArcType.Poke:
                    _sprite.Offset += (_weaponArc.Speed * frameTime, 0);
                    break;
            }


            if (_weaponArc.Length.TotalSeconds <= _timer)
            {
                Owner.Delete();
            }
        }
    }
}
