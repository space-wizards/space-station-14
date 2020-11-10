using Content.Shared.Eui;
using Content.Shared.Network.NetMessages;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;

#nullable enable

namespace Content.Client.Eui
{
    public abstract class BaseEui
    {
        [Dependency] private readonly IClientNetManager _netManager = default!;

        public EuiManager Manager { get; private set; } = default!;
        public uint Id { get; private set; }

        protected BaseEui()
        {
            IoCManager.InjectDependencies(this);
        }

        public void Initialize(EuiManager mgr, uint id)
        {
            Manager = mgr;
            Id = id;
        }

        public virtual void Opened()
        {
        }

        public virtual void Closed()
        {
        }

        public virtual void HandleState(EuiStateBase state)
        {
        }

        public virtual void HandleMessage(EuiMessageBase msg)
        {
        }

        protected void SendMessage(EuiMessageBase msg)
        {
            var netMsg = _netManager.CreateNetMessage<MsgEuiMessage>();
            netMsg.Id = Id;
            netMsg.Message = msg;
            
            _netManager.ClientSendMessage(netMsg);
        }
    }
}
