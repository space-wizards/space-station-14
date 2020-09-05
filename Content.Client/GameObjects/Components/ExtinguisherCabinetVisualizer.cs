using Content.Shared.GameObjects.Components;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;

namespace Content.Client.GameObjects.Components
{
    public class ExtinguisherCabinetVisualizer : AppearanceVisualizer
    {
        private string _prefix;

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            var sprite = component.Owner.GetComponent<ISpriteComponent>();

            if (component.TryGetData(ExtinguisherCabinetVisuals.IsOpen, out bool isOpen))
            {
                if (isOpen)
                {
                    if (component.TryGetData(ExtinguisherCabinetVisuals.ContainsExtinguisher, out bool contains))
                    {
                        if (contains)
                        {
                            sprite.LayerSetState(0, "extinguisher_full");
                        }
                        else
                        {
                            sprite.LayerSetState(0, "extinguisher_empty");
                        }

                    }
                }
                else
                {
                    sprite.LayerSetState(0, "extinguisher_closed");
                }
            }
        }
    }
}
