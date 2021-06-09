#nullable enable
using Content.Server.GameObjects.Components.Power.PowerNetComponents;
using Robust.Shared.GameObjects;
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

            PowerConsumerComponent = Owner.EnsureComponentWarn<PowerConsumerComponent>();
            PowerConsumerComponent.OnReceivedPowerChanged += PowerReceivedChanged;
        }

        private void PowerReceivedChanged(object? sender, ReceivedPowerChangedEventArgs e)
        {
            Master?.PowerBoxReceivedChanged(sender, e);
        }
    }
}
