using System;
using Content.Shared.GameObjects.Components.Weapons.Ranged;
using Content.Shared.Utility;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.Log;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Client.GameObjects.Components.Weapons.Ranged
{
    public sealed class BallisticMagazineWeaponVisualizer2D : AppearanceVisualizer
    {
        private string _baseState;
        private int _steps;

        public override void LoadData(YamlMappingNode node)
        {
            base.LoadData(node);

            _baseState = node.GetNode("base_state").AsString();
            _steps = node.GetNode("steps").AsInt();
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            var sprite = component.Owner.GetComponent<ISpriteComponent>();

            component.TryGetData(BallisticMagazineWeaponVisuals.MagazineLoaded, out bool loaded);

            if (loaded)
            {
                if (!component.TryGetData(BallisticMagazineWeaponVisuals.AmmoCapacity, out int capacity))
                {
                    return;
                }
                if (!component.TryGetData(BallisticMagazineWeaponVisuals.AmmoLeft, out int current))
                {
                    return;
                }

                // capacity is - 1 as normally a bullet is chambered so max state is virtually never hit.
                var step = ContentHelpers.RoundToLevels(current, capacity  - 1, _steps);
                sprite.LayerSetState(0, $"{_baseState}-{step}");
            }
            else
            {
                sprite.LayerSetState(0, _baseState);
            }
        }
    }
}
