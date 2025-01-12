using Content.Shared.Eui;
using Robust.Shared.IoC;
using Robust.Shared.Network;

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

        internal void Initialize(EuiManager mgr, uint id)
        {
            Manager = mgr;
            Id = id;
        }

        /// <summary>
        ///     Called when the EUI is opened by the server.
        /// </summary>
        public virtual void Opened()
        {
        }

        /// <summary>
        ///     Called when the EUI is closed by the server.
        /// </summary>
        public virtual void Closed()
        {
        }

        /// <summary>
        ///     Called when a new state comes in from the server.
        /// </summary>
        public virtual void HandleState(EuiStateBase state)
        {
        }

        /// <summary>
        ///     Called when a message comes in from the server.
        /// </summary>
        public virtual void HandleMessage(EuiMessageBase msg)
        {
        }

        /// <summary>
        ///     Send a message to the server-side implementation.
        /// </summary>
        protected void SendMessage(EuiMessageBase msg)
        {
            var netMsg = new MsgEuiMessage();
            netMsg.Id = Id;
            netMsg.Message = msg;

            _netManager.ClientSendMessage(netMsg);
        }
    }
}
