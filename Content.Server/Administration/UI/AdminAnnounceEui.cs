using Content.Server.Administration.Managers;
using Content.Server.Chat.Managers;
using Content.Server.EUI;
using Content.Server.Station.Systems;
using Content.Shared.Administration;
using Content.Shared.Eui;

namespace Content.Server.Administration.UI
{
    public sealed class AdminAnnounceEui : BaseEui
    {
        [Dependency] private readonly IAdminManager _adminManager = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly EntityManager _entityManager = default!;
        [Dependency] private readonly StationSystem _stationSystem = default!;

        public AdminAnnounceEui()
        {
            IoCManager.InjectDependencies(this);
        }

        public override void Opened()
        {
            StateDirty();
        }

        public override EuiStateBase GetNewState()
        {
            List<string> stationOptions = new List<string>();
            foreach (var station in _stationSystem.Stations)
            {
                stationOptions.Add(_entityManager.GetComponent<MetaDataComponent>(station).EntityName);
            }
            return new AdminAnnounceEuiState(stationOptions);
        }

        public override void HandleMessage(EuiMessageBase msg)
        {
            switch (msg)
            {
                case AdminAnnounceEuiMsg.Close:
                    Close();
                    break;
                case AdminAnnounceEuiMsg.DoAnnounce doAnnounce:
                    if (!_adminManager.HasAdminFlag(Player, AdminFlags.Admin))
                    {
                        Close();
                        break;
                    }

                    switch (doAnnounce.AnnounceType)
                    {
                        case AdminAnnounceType.Server:
                            _chatManager.DispatchServerAnnouncement(doAnnounce.Announcement);
                            break;
                        //case AdminAnnounceType.Station:
                        //    _chatManager.DispatchStationAnnouncement(doAnnounce.Announcement, doAnnounce.Announcer, colorOverride: Color.Gold);
                        //    break;
                    }

                    StateDirty();

                    if (doAnnounce.CloseAfter)
                        Close();

                    break;
            }
        }
    }
}
