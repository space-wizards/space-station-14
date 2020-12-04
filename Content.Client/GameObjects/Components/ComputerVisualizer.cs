using Content.Shared.GameObjects.Components;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Client.GameObjects.Components
{
    public sealed class ComputerVisualizer : AppearanceVisualizer
    {
        private string KeyboardState = "generic_key";
        private string ScreenState = "generic";
        private string BodyState = "computer";
        private string BodyBrokenState = "broken";
        private string ScreenBroken = "computer_broken";

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

            if (node.TryGetNode("body", out scalar))
            {
                BodyState = scalar.AsString();
            }

            if (node.TryGetNode("bodyBroken", out scalar))
            {
                BodyBrokenState = scalar.AsString();
            }
        }

        public override void InitializeEntity(IEntity entity)
        {
            base.InitializeEntity(entity);

            var sprite = entity.GetComponent<ISpriteComponent>();
            sprite.LayerSetState(Layers.Screen, ScreenState);

            if (!string.IsNullOrEmpty(KeyboardState))
            {
                sprite.LayerSetState(Layers.Keyboard, $"{KeyboardState}_off");
                sprite.LayerSetState(Layers.KeyboardOn, KeyboardState);
            }
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
                sprite.LayerSetState(Layers.Body, BodyBrokenState);
                sprite.LayerSetState(Layers.Screen, ScreenBroken);
            }
            else
            {
                sprite.LayerSetState(Layers.Body, BodyState);
                sprite.LayerSetState(Layers.Screen, ScreenState);
            }

            sprite.LayerSetVisible(Layers.Screen, powered);
            if (sprite.LayerMapTryGet(Layers.KeyboardOn, out _))
            {
                sprite.LayerSetVisible(Layers.KeyboardOn, powered);
            }
        }

        public enum Layers : byte
        {
            Body,
            Screen,
            Keyboard,
            KeyboardOn
        }
    }
}
