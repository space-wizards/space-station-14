using System;
using System.Collections.Generic;
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
            switch (obj.Message)
            {
                case ParticleAcceleratorSetEnableMessage enableMessage:
                    if(ParticleAccelerator.Enabled == enableMessage.Enabled) break;

                    ParticleAccelerator.Enabled = enableMessage.Enabled;
                    break;
                case ParticleAcceleratorSetPowerStateMessage stateMessage:
                    if (ParticleAccelerator.Power == stateMessage.State) break;

                    ParticleAccelerator.Power = stateMessage.State;
                    break;
            }
        }

        public override ParticleAcceleratorPartComponent[] GetNeighbours()
        {
            return new ParticleAcceleratorPartComponent[]
            {
                ParticleAccelerator.FuelChamber
            };
        }

        protected override void RegisterAtParticleAccelerator()
        {
            ParticleAccelerator.ControlBox = this;
        }

        protected override void UnRegisterAtParticleAccelerator()
        {
            ParticleAccelerator.ControlBox = null;
        }

        public void OnParticleAcceleratorValuesChanged()
        {
            //adjust power drain
            //todo
            //update ui
            UserInterface?.SendMessage(ParticleAccelerator.DataMessage);
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
            if (UserInterface?.SessionHasOpen(actor.playerSession) == true)
            {
                UserInterface?.SendMessage(ParticleAccelerator.DataMessage, actor.playerSession);
            }

            return true;
        }

        public override void OnRemove()
        {
            UserInterface?.CloseAll();
            base.OnRemove();
        }
    }
}
