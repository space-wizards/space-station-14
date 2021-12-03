using Content.Shared.PowerCell;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Client.PowerCell
{
    [UsedImplicitly]
    public class PowerCellVisualizer : AppearanceVisualizer
    {
        [DataField("prefix")]
        private string? _prefix;

        public override void InitializeEntity(IEntity entity)
        {
            base.InitializeEntity(entity);

            var sprite = IoCManager.Resolve<IEntityManager>().GetComponent<ISpriteComponent>(entity.Uid);

            if (_prefix != null)
            {
                sprite.LayerMapSet(Layers.Charge, sprite.AddLayerState($"{_prefix}_100"));
                sprite.LayerSetShader(Layers.Charge, "unshaded");
            }
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            var sprite = IoCManager.Resolve<IEntityManager>().GetComponent<ISpriteComponent>(component.Owner.Uid);
            if (component.TryGetData(PowerCellVisuals.ChargeLevel, out byte level))
            {
                var adjustedLevel = level * 25;
                sprite.LayerSetState(Layers.Charge, $"{_prefix}_{adjustedLevel}");
            }
        }

        private enum Layers : byte
        {
            Charge
        }
    }
}
