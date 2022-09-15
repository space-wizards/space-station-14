using Content.Shared.Computer;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Client.Computer
{
    [UsedImplicitly]
    public sealed class ComputerVisualizer : AppearanceVisualizer
    {
        [DataField("key")]
        private string KeyboardState = "generic_key";
        [DataField("screen")]
        private string ScreenState = "generic";
        [DataField("body")]
        private string BodyState = "computer";
        [DataField("bodyBroken")]
        private string BodyBrokenState = "broken";
        private string ScreenBroken = "computer_broken";

        [Obsolete("Subscribe to your component being initialised instead.")]
        public override void InitializeEntity(EntityUid entity)
        {
            base.InitializeEntity(entity);

            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent<SpriteComponent>(entity, out var sprite)) return;

            sprite.LayerSetState(Layers.Screen, ScreenState);

            if (!string.IsNullOrEmpty(KeyboardState))
            {
                sprite.LayerSetState(Layers.Keyboard, $"{KeyboardState}_off");
                sprite.LayerSetState(Layers.KeyboardOn, KeyboardState);
            }
        }

        [Obsolete("Subscribe to AppearanceChangeEvent instead.")]
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent<SpriteComponent>(component.Owner, out var sprite)) return;

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
