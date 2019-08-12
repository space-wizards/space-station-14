using Content.Shared.GameObjects.Components;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Client.GameObjects.Components
{
    public sealed class ComputerVisualizer2D : AppearanceVisualizer
    {
        private string KeyboardState = "generic_key";
        private string ScreenState = "generic";

        public override void LoadData(YamlMappingNode node)
        {
            base.LoadData(node);

            if (node.TryGetNode("key", out var scalar))
            {
                KeyboardState = scalar.AsString();
            }

            if (node.TryGetNode("screen", out scalar))
            {
                ScreenState = scalar.AsString();
            }
        }

        public override void InitializeEntity(IEntity entity)
        {
            base.InitializeEntity(entity);

            var sprite = entity.GetComponent<ISpriteComponent>();
            sprite.LayerSetState(Layers.Screen, ScreenState);
            sprite.LayerSetState(Layers.Keyboard, $"{KeyboardState}_off");
            sprite.LayerSetState(Layers.KeyboardOn, KeyboardState);
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            var sprite = component.Owner.GetComponent<ISpriteComponent>();

            if (!component.TryGetData(ComputerVisuals.Powered, out bool powered))
            {
                powered = true;
            }

            component.TryGetData(ComputerVisuals.Broken, out bool broken);

            if (broken)
            {
                sprite.LayerSetState(Layers.Body, "broken");
                sprite.LayerSetState(Layers.Screen, "computer_broken");
            }
            else
            {
                sprite.LayerSetState(Layers.Body, "computer");
                sprite.LayerSetState(Layers.Screen, ScreenState);
            }

            sprite.LayerSetVisible(Layers.Screen, powered);
            sprite.LayerSetVisible(Layers.KeyboardOn, powered);
        }

        public enum Layers
        {
            Body,
            Screen,
            Keyboard,
            KeyboardOn
        }
    }
}
