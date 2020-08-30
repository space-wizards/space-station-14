using System;
using Content.Shared.GameObjects.Components.Medical;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;
using static Content.Shared.GameObjects.Components.Medical.SharedCloningMachineComponent;
using static Content.Shared.GameObjects.Components.Medical.SharedCloningMachineComponent.CloningMachineStatus;

namespace Content.Client.GameObjects.Components.CloningMachine
{
    public class CloningMachineVisualizer : AppearanceVisualizer
    {
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            var sprite = component.Owner.GetComponent<ISpriteComponent>();
            if (!component.TryGetData(CloningMachineVisuals.Status, out CloningMachineStatus status)) return;
            sprite.LayerSetState(CloningMachineVisualLayers.Machine, StatusToMachineStateId(status));
        }

        private string StatusToMachineStateId(CloningMachineStatus status)
        {
            //TODO: implement NoMind for if the mind is not yet in the body
            //TODO: Find a use for GORE POD
            switch (status)
            {
                case Cloning: return "pod_1";
                case NoMind: return "pod_e";
                case Gore: return "pod_g";
                case Idle: return "pod_0";
                default:
                    throw new ArgumentOutOfRangeException(nameof(status), status, "unknown MedicalScannerStatus");
            }
        }

        public enum CloningMachineVisualLayers
        {
            Machine,
        }
    }
}
