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

        private MeleeWeaponAnimationPrototype _meleeWeaponAnimation;

        private float _timer;
        private SpriteComponent _sprite;
        private Angle _baseAngle;

        public override void Initialize()
        {
            base.Initialize();

            _sprite = Owner.GetComponent<SpriteComponent>();
        }

        public void SetData(MeleeWeaponAnimationPrototype prototype, Angle baseAngle, IEntity attacker, bool followAttacker = true)
        {
            _meleeWeaponAnimation = prototype;
            _sprite.AddLayer(new RSI.StateId(prototype.State));
            _baseAngle = baseAngle;
            if(followAttacker)
                Owner.Transform.AttachParent(attacker);
        }

        internal void Update(float frameTime)
        {
            if (_meleeWeaponAnimation == null)
            {
                return;
            }

            _timer += frameTime;

            var (r, g, b, a) =
                Vector4.Clamp(_meleeWeaponAnimation.Color + _meleeWeaponAnimation.ColorDelta * _timer, Vector4.Zero, Vector4.One);
            _sprite.Color = new Color(r, g, b, a);

            switch (_meleeWeaponAnimation.ArcType)
            {
                case WeaponArcType.Slash:
                    var angle = Angle.FromDegrees(_meleeWeaponAnimation.Width)/2;
                    Owner.Transform.WorldRotation =
                        _baseAngle + Angle.Lerp(-angle, angle, (float) (_timer / _meleeWeaponAnimation.Length.TotalSeconds));
                    break;

                case WeaponArcType.Poke:
                    Owner.Transform.WorldRotation = _baseAngle;
                    _sprite.Offset -= (0, _meleeWeaponAnimation.Speed * frameTime);
                    break;
            }


            if (_meleeWeaponAnimation.Length.TotalSeconds <= _timer)
            {
                Owner.Delete();
            }
        }
    }
}
