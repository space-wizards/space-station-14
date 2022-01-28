using System.Collections.Generic;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Sprite.Components
{
    [RegisterComponent]
    public class RandomSpriteColorComponent : Component, IMapInit
    {
        public override string Name => "RandomSpriteColor";

        [DataField("selected")]
        private string? _selectedColor;
        [DataField("state")]
        private string _baseState = "error";

        [DataField("colors")] private readonly Dictionary<string, Color> _colors = new();

        void IMapInit.MapInit()
        {
            var random = IoCManager.Resolve<IRobustRandom>();
            _selectedColor = random.Pick(_colors.Keys);
            UpdateColor();
        }

        protected override void Startup()
        {
            base.Startup();

            UpdateColor();
        }

        private void UpdateColor()
        {
            if (IoCManager.Resolve<IEntityManager>().TryGetComponent(Owner, out SpriteComponent? spriteComponent) && _selectedColor != null)
            {
                spriteComponent.LayerSetState(0, _baseState);
                spriteComponent.LayerSetColor(0, _colors[_selectedColor]);
            }
        }
    }
}
