using Content.Shared.Weapons.Melee;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Content.Client.Weapons.Melee.Components
{
    [RegisterComponent]
    public sealed class MeleeWeaponArcAnimationComponent : Component
    {
        [Dependency] private readonly IEntityManager _entMan = default!;
        private MeleeWeaponAnimationPrototype? _meleeWeaponAnimation;

        private float _timer;
        private SpriteComponent? _sprite;
        private Angle _baseAngle;

        protected override void Initialize()
        {
            base.Initialize();

            _sprite = _entMan.GetComponent<SpriteComponent>(Owner);
        }

        public void SetData(MeleeWeaponAnimationPrototype prototype, Angle baseAngle, EntityUid attacker, bool followAttacker = true)
        {
            _meleeWeaponAnimation = prototype;
            _sprite?.AddLayer(new RSI.StateId(prototype.State));
            _baseAngle = baseAngle;
            if(followAttacker)
                _entMan.GetComponent<TransformComponent>(Owner).AttachParent(attacker);
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

            if (_sprite != null)
            {
                _sprite.Color = new Color(r, g, b, a);
            }

            var transform = _entMan.GetComponent<TransformComponent>(Owner);

            switch (_meleeWeaponAnimation.ArcType)
            {
                case WeaponArcType.Slash:
                    var angle = Angle.FromDegrees(_meleeWeaponAnimation.Width)/2;
                    transform.WorldRotation = _baseAngle + Angle.Lerp(-angle, angle, (float) (_timer / _meleeWeaponAnimation.Length.TotalSeconds));
                    break;

                case WeaponArcType.Poke:
                    transform.WorldRotation = _baseAngle;

                    if (_sprite != null)
                    {
                        _sprite.Offset -= (0, _meleeWeaponAnimation.Speed * frameTime);
                    }
                    break;
            }


            if (_meleeWeaponAnimation.Length.TotalSeconds <= _timer)
            {
                _entMan.DeleteEntity(Owner);
            }
        }
    }
}
