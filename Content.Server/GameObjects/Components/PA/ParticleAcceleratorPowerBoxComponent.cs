#nullable enable
using Content.Server.GameObjects.Components.Power.PowerNetComponents;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.PA
{
    [RegisterComponent]
    [ComponentReference(typeof(ParticleAcceleratorPartComponent))]
    public class ParticleAcceleratorPowerBoxComponent : ParticleAcceleratorPartComponent
    {
        public override string Name => "ParticleAcceleratorPowerBox";
        [ViewVariables] public PowerConsumerComponent? PowerConsumerComponent;

        public override void Initialize()
        {
            base.Initialize();
            if (Owner.TryGetComponent(out PowerConsumerComponent))
            {
                PowerConsumerComponent.OnReceivedPowerChanged += PowerReceivedChanged;
                return;
            }

            Logger.Error($"ParticleAcceleratorPowerBoxComponent Component initialized without PowerConsumerComponent.");
        }

        private void PowerReceivedChanged(object? sender, ReceivedPowerChangedEventArgs e)
        {
            Master?.PowerBoxReceivedChanged(sender, e);
        }
    }
}
