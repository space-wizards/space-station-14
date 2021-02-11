using System.Collections.Generic;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Random;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components
{
    [RegisterComponent]
    public class RandomSpriteColorComponent : Component, IMapInit
    {
        public override string Name => "RandomSpriteColor";

        private string _selectedColor;
        private string _baseState;
        private Dictionary<string, Color> _colors;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _selectedColor, "selected", null);
            serializer.DataField(ref _baseState, "state", "error");
            serializer.DataFieldCached(ref _colors, "colors", new Dictionary<string, Color>());
        }

        void IMapInit.MapInit()
        {
            if (_colors == null)
            {
                return;
            }

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
            if (Owner.TryGetComponent(out SpriteComponent spriteComponent) && _colors != null && _selectedColor != null)
            {
                spriteComponent.LayerSetState(0, _baseState);
                spriteComponent.LayerSetColor(0, _colors[_selectedColor]);
            }
        }
    }
}
