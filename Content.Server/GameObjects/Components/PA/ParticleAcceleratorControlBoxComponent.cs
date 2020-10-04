using System;
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

        public void SetPowerLevel(ParticleAccelerator.ParticleAcceleratorPowerState level)
        {
            ParticleAccelerator.Power = level;
        }

        public bool InteractHand(InteractHandEventArgs eventArgs)
        {
            switch (ParticleAccelerator.Power)
            {
                case ParticleAccelerator.ParticleAcceleratorPowerState.Off:
                    SetPowerLevel(ParticleAccelerator.ParticleAcceleratorPowerState.Powered);
                    break;
                case ParticleAccelerator.ParticleAcceleratorPowerState.Powered:
                    SetPowerLevel(ParticleAccelerator.ParticleAcceleratorPowerState.Level0);
                    break;
                case ParticleAccelerator.ParticleAcceleratorPowerState.Level0:
                    SetPowerLevel(ParticleAccelerator.ParticleAcceleratorPowerState.Level1);
                    break;
                case ParticleAccelerator.ParticleAcceleratorPowerState.Level1:
                    SetPowerLevel(ParticleAccelerator.ParticleAcceleratorPowerState.Level2);
                    break;
                case ParticleAccelerator.ParticleAcceleratorPowerState.Level2:
                    SetPowerLevel(ParticleAccelerator.ParticleAcceleratorPowerState.Level3);
                    break;
                case ParticleAccelerator.ParticleAcceleratorPowerState.Level3:
                    SetPowerLevel(ParticleAccelerator.ParticleAcceleratorPowerState.Powered);
                    break;
            }
            Owner.PopupMessage(eventArgs.User, Loc.GetString($"Set Power to {ParticleAccelerator.Power}"));
            return true;
        }
    }
}
