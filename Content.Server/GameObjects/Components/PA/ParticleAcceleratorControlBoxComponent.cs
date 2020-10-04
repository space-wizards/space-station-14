using System;
using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using Content.Server.Utility;
using Content.Shared.Arcade;
using Content.Shared.GameObjects.Components;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;

namespace Content.Server.GameObjects.Components.PA
{
    [RegisterComponent]
    public class ParticleAcceleratorControlBoxComponent : ParticleAcceleratorPartComponent, IInteractHand
    {
        public override string Name => "ParticleAcceleratorControlBox";

        private BoundUserInterface? UserInterface => Owner.GetUIOrNull(ParticleAcceleratorControlBoxUiKey.Key);

        private bool Powered => !Owner.TryGetComponent(out PowerReceiverComponent? receiver) || receiver.Powered;

        public override void Initialize()
        {
            base.Initialize();
            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += UserInterfaceOnOnReceiveMessage;
            }
        }

        private void UserInterfaceOnOnReceiveMessage(ServerBoundUserInterfaceMessage obj)
        {
            if (ParticleAccelerator.Power == ParticleAcceleratorPowerState.Off) return;

            switch (obj.Message)
            {
                case ParticleAcceleratorTogglePowerMessage _:
                    ParticleAccelerator.Power = ParticleAccelerator.Power == ParticleAcceleratorPowerState.Powered
                        ? ParticleAcceleratorPowerState.Level0
                        : ParticleAcceleratorPowerState.Powered;
                    break;
                case ParticleAcceleratorIncreasePowerMessage _:
                    if (ParticleAccelerator.Power == ParticleAcceleratorPowerState.Level3) break;

                    ParticleAccelerator.Power++;
                    break;
                case ParticleAcceleratorDecreasePowerMessage _:
                    if (ParticleAccelerator.Power == ParticleAcceleratorPowerState.Powered) break;

                    ParticleAccelerator.Power--;
                    break;
            }
        }

        protected override void RegisterAtParticleAccelerator()
        {
            ParticleAccelerator.ControlBox = this;
        }

        protected override void UnRegisterAtParticleAccelerator()
        {
            ParticleAccelerator.ControlBox = null;
        }

        public void PowerLevelUpdated()
        {
            //adjust power drain
            //todo
            //update ui
            UserInterface?.SendMessage(new ParticleAcceleratorDataUpdateMessage(ParticleAccelerator.IsFunctional(), ParticleAccelerator.Power));
        }

        public bool InteractHand(InteractHandEventArgs eventArgs)
        {
            if(!eventArgs.User.TryGetComponent(out IActorComponent? actor))
            {
                return false;
            }
            if (!Powered)
            {
                return false;
            }
            if(!ActionBlockerSystem.CanInteract(Owner)) return false;

            UserInterface?.Toggle(actor.playerSession);
            UserInterface?.SendMessage(new ParticleAcceleratorDataUpdateMessage(ParticleAccelerator.IsFunctional(), ParticleAccelerator.Power), actor.playerSession);

            return true;
        }
    }
}
