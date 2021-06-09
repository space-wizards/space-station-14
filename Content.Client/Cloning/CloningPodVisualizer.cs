using System;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using static Content.Shared.Cloning.SharedCloningPodComponent;
using static Content.Shared.Cloning.SharedCloningPodComponent.CloningPodStatus;

namespace Content.Client.Cloning
{
    [UsedImplicitly]
    public class CloningPodVisualizer : AppearanceVisualizer
    {
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            var sprite = component.Owner.GetComponent<ISpriteComponent>();
            if (!component.TryGetData(CloningPodVisuals.Status, out CloningPodStatus status)) return;
            sprite.LayerSetState(CloningPodVisualLayers.Machine, StatusToMachineStateId(status));
        }

        private string StatusToMachineStateId(CloningPodStatus status)
        {
            //TODO: implement NoMind for if the mind is not yet in the body
            //TODO: Find a use for GORE POD
            switch (status)
            {
                case CloningPodStatus.Cloning: return "pod_1";
                case NoMind: return "pod_e";
                case Gore: return "pod_g";
                case Idle: return "pod_0";
                default:
                    throw new ArgumentOutOfRangeException(nameof(status), status, "unknown CloningPodStatus");
            }
        }

        public enum CloningPodVisualLayers : byte
        {
            Machine,
        }
    }
}
