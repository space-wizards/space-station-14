using System;
using Content.Shared.Eui;
using Content.Shared.Network.NetMessages;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;

#nullable enable

namespace Content.Server.Eui
{
    public abstract class BaseEui
    {
        private bool _isStateDirty = false;

        public bool IsShutDown { get; private set; }
        public EuiManager Manager { get; private set; } = default!;
        public IPlayerSession Player { get; private set; } = default!;
        public uint Id { get; private set; }

        public void Initialize(EuiManager manager, IPlayerSession player, uint id)
        {
            Manager = manager;
            Player = player;
            Id = id;
            Opened();
        }

        public virtual void Opened()
        {

        }

        public virtual void Closed()
        {

        }

        public virtual void HandleMessage(EuiMessageBase msg)
        {
        }

        public void Shutdown()
        {
            Closed();
            IsShutDown = true;
        }

        /// <summary>
        ///     Mark the current UI state as dirty and queue for an update.
        /// </summary>
        public void StateDirty()
        {
            if (_isStateDirty)
            {
                return;
            }

            _isStateDirty = true;
            Manager.QueueStateUpdate(this);
        }

        public virtual EuiStateBase GetNewState()
        {
            throw new NotSupportedException();
        }

        public void Close()
        {
            Manager.CloseEui(this);
        }

        public void DoStateUpdate()
        {
            _isStateDirty = false;

            var state = GetNewState();

            var netMgr = IoCManager.Resolve<IServerNetManager>();
            var msg = netMgr.CreateNetMessage<MsgEuiState>();
            msg.Id = Id;
            msg.State = state;

            netMgr.ServerSendMessage(msg, Player.ConnectedClient);
        }

        public void SendMessage(EuiMessageBase message)
        {
            var netMgr = IoCManager.Resolve<IServerNetManager>();
            var msg = netMgr.CreateNetMessage<MsgEuiMessage>();
            msg.Id = Id;
            msg.Message = message;

            netMgr.ServerSendMessage(msg, Player.ConnectedClient);
        }
    }
}
