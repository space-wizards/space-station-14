using Content.Shared.Eui;
using Robust.Shared.Network;
using Robust.Shared.Reflection;

namespace Content.Client.Eui
{
    public sealed class EuiManager
    {
        [Dependency] private readonly IClientNetManager _net = default!;
        [Dependency] private readonly IReflectionManager _refl = default!;
        [Dependency] private readonly IDynamicTypeFactory _dtf = default!;

        private readonly Dictionary<uint, EuiData> _openUis = new();

        public void Initialize()
        {
            _net.RegisterNetMessage<MsgEuiCtl>(RxMsgCtl);
            _net.RegisterNetMessage<MsgEuiState>(RxMsgState);
            _net.RegisterNetMessage<MsgEuiMessage>(RxMsgMessage);
            _net.Disconnect += NetOnDisconnect;
        }

        private void NetOnDisconnect(object? sender, NetDisconnectedArgs e)
        {
            foreach (var openUi in _openUis)
            {
                openUi.Value.Eui.Closed();
            }
            _openUis.Clear();
        }

        private void RxMsgMessage(MsgEuiMessage message)
        {
            var ui = _openUis[message.Id].Eui;
            ui.HandleMessage(message.Message);
        }

        private void RxMsgState(MsgEuiState message)
        {
            var ui = _openUis[message.Id].Eui;
            ui.HandleState(message.State);
        }

        private void RxMsgCtl(MsgEuiCtl message)
        {
            // Will always close the window first when getting a control message
            if (_openUis.TryGetValue(message.Id, out var openEui))
            {
                openEui.Eui.Closed();
                _openUis.Remove(message.Id);
            }

            if (message.Type != MsgEuiCtl.CtlType.Open)
                return;

            // Will open/re-open the window if the server wants the eui opened.
            var euiType = _refl.LooseGetType(message.OpenType);
            var instance = _dtf.CreateInstance<BaseEui>(euiType);
            instance.Initialize(this, message.Id);
            instance.Opened();
            _openUis.Add(message.Id, new EuiData(instance));
        }

        private sealed class EuiData
        {
            public readonly BaseEui Eui;

            public EuiData(BaseEui eui)
            {
                Eui = eui;
            }
        }
    }
}
