#nullable enable
using Content.Server.Power.Components;
using Content.Server.UserInterface;
using Content.Shared.Acts;
using Content.Shared.Gravity;
using Content.Shared.Interaction;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Gravity
{
    [RegisterComponent]
    public class GravityGeneratorComponent : SharedGravityGeneratorComponent, IBreakAct, IInteractHand
    {
        [ComponentDependency] private readonly AppearanceComponent? _appearance = default!;

        [DataField("switchedOn")]
        private bool _switchedOn = true;

        [DataField("intact")]
        private bool _intact = true;

        private GravityGeneratorStatus _status;

        public bool Powered => !Owner.TryGetComponent(out PowerReceiverComponent? receiver) || receiver.Powered;

        public bool SwitchedOn => _switchedOn;

        public bool Intact => _intact;

        public GravityGeneratorStatus Status => _status;

        public bool NeedsUpdate
        {
            get
            {
                switch (_status)
                {
                    case GravityGeneratorStatus.On:
                        return !(Powered && SwitchedOn && Intact);
                    case GravityGeneratorStatus.Off:
                        return SwitchedOn || !(Powered && Intact);
                    case GravityGeneratorStatus.Unpowered:
                        return SwitchedOn || Powered || !Intact;
                    case GravityGeneratorStatus.Broken:
                        return SwitchedOn || Powered || Intact;
                    default:
                        return true; // This _should_ be unreachable
                }
            }
        }

        public override string Name => "GravityGenerator";

        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(GravityGeneratorUiKey.Key);

        protected override void Initialize()
        {
            base.Initialize();

            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += HandleUIMessage;
            }

            _switchedOn = true;
            _intact = true;
            _status = GravityGeneratorStatus.On;
            UpdateState();
        }

        bool IInteractHand.InteractHand(InteractHandEventArgs eventArgs)
        {
            if (!eventArgs.User.TryGetComponent<ActorComponent>(out var actor))
                return false;
            if (Status != GravityGeneratorStatus.Off && Status != GravityGeneratorStatus.On)
            {
                return false;
            }
            OpenUserInterface(actor.PlayerSession);
            return true;
        }

        public void OnBreak(BreakageEventArgs eventArgs)
        {
            _intact = false;
            _switchedOn = false;
        }

        public void UpdateState()
        {
            if (!Intact)
            {
                MakeBroken();
            }
            else if (!Powered)
            {
                MakeUnpowered();
            }
            else if (!SwitchedOn)
            {
                MakeOff();
            }
            else
            {
                MakeOn();
            }
        }

        private void HandleUIMessage(ServerBoundUserInterfaceMessage message)
        {
            switch (message.Message)
            {
                case GeneratorStatusRequestMessage _:
                    UserInterface?.SetState(new GeneratorState(Status == GravityGeneratorStatus.On));
                    break;
                case SwitchGeneratorMessage msg:
                    _switchedOn = msg.On;
                    UpdateState();
                    break;
                default:
                    break;
            }
        }

        private void OpenUserInterface(IPlayerSession playerSession)
        {
            UserInterface?.Open(playerSession);
        }

        private void MakeBroken()
        {
            _status = GravityGeneratorStatus.Broken;

            _appearance?.SetData(GravityGeneratorVisuals.State, Status);
            _appearance?.SetData(GravityGeneratorVisuals.CoreVisible, false);
        }

        private void MakeUnpowered()
        {
            _status = GravityGeneratorStatus.Unpowered;

            _appearance?.SetData(GravityGeneratorVisuals.State, Status);
            _appearance?.SetData(GravityGeneratorVisuals.CoreVisible, false);
        }

        private void MakeOff()
        {
            _status = GravityGeneratorStatus.Off;

            _appearance?.SetData(GravityGeneratorVisuals.State, Status);
            _appearance?.SetData(GravityGeneratorVisuals.CoreVisible, false);
        }

        private void MakeOn()
        {
            _status = GravityGeneratorStatus.On;

            _appearance?.SetData(GravityGeneratorVisuals.State, Status);
            _appearance?.SetData(GravityGeneratorVisuals.CoreVisible, true);
        }
    }
}
