using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;

namespace Content.Server.GameObjects.Components.PA
{
    [RegisterComponent]
    public class ParticleAcceleratorControlBoxComponent : ParticleAcceleratorPartComponent, IInteractHand
    {
        public override string Name => "ParticleAcceleratorControlBox";

        protected override void RegisterAtParticleAccelerator()
        {
            ParticleAccelerator.ControlBox = this;
        }

        protected override void UnRegisterAtParticleAccelerator()
        {
            ParticleAccelerator.ControlBox = null;
        }

        public bool InteractHand(InteractHandEventArgs eventArgs)
        {
            ParticleAccelerator.Power = ParticleAccelerator.ParticleAcceleratorPowerState.Level1;
            Owner.PopupMessage(eventArgs.User, Loc.GetString($"Debug hurr durr {ParticleAccelerator.Power}"));
            return true;
        }
    }
}
