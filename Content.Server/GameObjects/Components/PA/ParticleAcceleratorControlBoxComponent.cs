#nullable enable
using System;
using System.Collections.Generic;
using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using Content.Server.GameObjects.Components.VendingMachines;
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
using Robust.Shared.Log;

namespace Content.Server.GameObjects.Components.PA
{
    [RegisterComponent]
    public class ParticleAcceleratorControlBoxComponent : ParticleAcceleratorPartComponent, IInteractHand, IWires
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
            if (ParticleAccelerator == null)
            {
                Logger.Error($"UserInterfaceOnOnReceiveMessage got called on {this} without a Particleaccelerator attached");
                return;
            }
            if(obj.Session.AttachedEntity == null || !ActionBlockerSystem.CanInteract(obj.Session.AttachedEntity)) return;
            if(ParticleAccelerator.WireFlagInterfaceBlock) return;
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
                ParticleAccelerator?.FuelChamber!
            };
        }

        protected override void RegisterAtParticleAccelerator()
        {
            if (ParticleAccelerator != null) ParticleAccelerator.ControlBox = this;
        }

        protected override void UnRegisterAtParticleAccelerator()
        {
            if (ParticleAccelerator != null) ParticleAccelerator.ControlBox = null;
        }

        public void OnParticleAcceleratorValuesChanged()
        {
            if (ParticleAccelerator == null)
            {
                Logger.Error($"OnParticleAcceleratorValuesChanged got called on {this} without a Particleaccelerator attached");
                return;
            }
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
            if(!ActionBlockerSystem.CanInteract(eventArgs.User)) return false;


            var wires = Owner.GetComponent<WiresComponent>();
            if (wires.IsPanelOpen)
            {
                wires.OpenInterface(actor.playerSession);
            } else
            {
                UserInterface?.Toggle(actor.playerSession);
                if (UserInterface?.SessionHasOpen(actor.playerSession) == true && ParticleAccelerator != null)
                {
                    UserInterface?.SendMessage(ParticleAccelerator.DataMessage, actor.playerSession);
                }
            }
            return true;
        }

        public override void OnRemove()
        {
            UserInterface?.CloseAll();
            base.OnRemove();
        }

        public void RegisterWires(WiresComponent.WiresBuilder builder)
        {
            builder.CreateWire(ParticleAcceleratorControlBoxWires.Toggle);
            builder.CreateWire(ParticleAcceleratorControlBoxWires.Strength);
            builder.CreateWire(ParticleAcceleratorControlBoxWires.Interface);
            builder.CreateWire(ParticleAcceleratorControlBoxWires.Limiter);
            builder.CreateWire(ParticleAcceleratorControlBoxWires.Nothing);
        }

        public void WiresUpdate(WiresUpdateEventArgs args)
        {
            switch(args.Identifier)
            {
                case ParticleAcceleratorControlBoxWires.Toggle:
                    if(ParticleAccelerator == null) return;
                    if (args.Action == SharedWiresComponent.WiresAction.Pulse)
                    {
                        ParticleAccelerator.Enabled = !ParticleAccelerator.Enabled;
                    }
                    else
                    {
                        ParticleAccelerator.WireFlagPowerBlock = args.Action == SharedWiresComponent.WiresAction.Cut;
                    }
                    break;
                case ParticleAcceleratorControlBoxWires.Strength:
                    if (args.Action == SharedWiresComponent.WiresAction.Pulse && ParticleAccelerator != null) ParticleAccelerator.Power++;
                    break;
                case ParticleAcceleratorControlBoxWires.Interface:
                    if (ParticleAccelerator == null) return;
                    if (args.Action == SharedWiresComponent.WiresAction.Pulse)
                    {
                        if(ParticleAccelerator.WireFlagInterfaceBlock) return; //we dont want pulse to enable it after it has been cut

                        ParticleAccelerator.WireFlagInterfaceBlock = !ParticleAccelerator.WireFlagInterfaceBlock;
                    }else
                    {
                        ParticleAccelerator.WireFlagInterfaceBlock =
                            args.Action == SharedWiresComponent.WiresAction.Cut;
                    }
                    break;
                case ParticleAcceleratorControlBoxWires.Limiter:
                    if (ParticleAccelerator == null) return;
                    if (args.Action == SharedWiresComponent.WiresAction.Pulse)
                    {
                        Owner.PopupMessageEveryone(Loc.GetString("The Controlbox makes a whirring noise."));
                    }
                    else
                    {
                        ParticleAccelerator.WireFlagMaxPower = args.Action == SharedWiresComponent.WiresAction.Cut
                            ? ParticleAcceleratorPowerState.Level3
                            : ParticleAcceleratorPowerState.Level2;
                    }
                    break;
            }
        }

        public enum ParticleAcceleratorControlBoxWires
        {
            /// <summary>
            /// Pulse toggles Power. Cut permanently turns off until Mend.
            /// </summary>
            Toggle,
            /// <summary>
            /// Pulsing increases level until at limit.
            /// </summary>
            Strength,
            /// <summary>
            /// Pulsing toggles Button-Disabled on UI. Cut disables, Mend enables.
            /// </summary>
            Interface,
            /// <summary>
            /// Pulsing will produce short message about whirring noise. Cutting increases the max level to 3. Mending reduces it back to 2.
            /// </summary>
            Limiter,
            /// <summary>
            /// Does Nothing
            /// </summary>
            Nothing
        }
    }
}
