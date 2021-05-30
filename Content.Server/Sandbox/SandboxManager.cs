using System.Linq;
using Content.Server.GameObjects.Components.Access;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.PDA;
using Content.Server.GameTicking;
using Content.Server.Interfaces.GameTicking;
using Content.Shared.Access;
using Content.Shared.Sandbox;
using Robust.Server.Console;
using Robust.Server.GameObjects;
using Robust.Server.Placement;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.ViewVariables;
using static Content.Shared.GameObjects.Components.Inventory.EquipmentSlotDefines;

namespace Content.Server.Sandbox
{
    internal sealed class SandboxManager : SharedSandboxManager, ISandboxManager
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IServerNetManager _netManager = default!;
        [Dependency] private readonly IGameTicker _gameTicker = default!;
        [Dependency] private readonly IPlacementManager _placementManager = default!;
        [Dependency] private readonly IConGroupController _conGroupController = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IServerConsoleHost _host = default!;

        private bool _isSandboxEnabled;

        [ViewVariables(VVAccess.ReadWrite)]
        public bool IsSandboxEnabled
        {
            get => _isSandboxEnabled;
            set
            {
                _isSandboxEnabled = value;
                UpdateSandboxStatusForAll();
            }
        }

        public void Initialize()
        {
            _netManager.RegisterNetMessage<MsgSandboxStatus>(nameof(MsgSandboxStatus));
            _netManager.RegisterNetMessage<MsgSandboxRespawn>(nameof(MsgSandboxRespawn), SandboxRespawnReceived);
            _netManager.RegisterNetMessage<MsgSandboxGiveAccess>(nameof(MsgSandboxGiveAccess), SandboxGiveAccessReceived);
            _netManager.RegisterNetMessage<MsgSandboxGiveAghost>(nameof(MsgSandboxGiveAghost), SandboxGiveAghostReceived);
            _netManager.RegisterNetMessage<MsgSandboxSuicide>(nameof(MsgSandboxSuicide), SandboxSuicideReceived);

            _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
            _gameTicker.OnRunLevelChanged += GameTickerOnOnRunLevelChanged;

            _placementManager.AllowPlacementFunc = placement =>
            {
                if (IsSandboxEnabled)
                {
                    return true;
                }

                var channel = placement.MsgChannel;
                var player = _playerManager.GetSessionByChannel(channel);

                if (_conGroupController.CanAdminPlace(player))
                {
                    return true;
                }

                return false;
            };
        }

        private void GameTickerOnOnRunLevelChanged(GameRunLevelChangedEventArgs obj)
        {
            // Automatically clear sandbox state when round resets.
            if (obj.NewRunLevel == GameRunLevel.PreRoundLobby)
            {
                IsSandboxEnabled = false;
            }
        }

        private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
        {
            if (e.NewStatus != SessionStatus.Connected || e.OldStatus != SessionStatus.Connecting)
            {
                return;
            }

            var msg = _netManager.CreateNetMessage<MsgSandboxStatus>();
            msg.SandboxAllowed = IsSandboxEnabled;
            _netManager.ServerSendMessage(msg, e.Session.ConnectedClient);
        }

        private void SandboxRespawnReceived(MsgSandboxRespawn message)
        {
            if (!IsSandboxEnabled)
            {
                return;
            }

            var player = _playerManager.GetSessionByChannel(message.MsgChannel);
            _gameTicker.Respawn(player);
        }

        private void SandboxGiveAccessReceived(MsgSandboxGiveAccess message)
        {
            if (!IsSandboxEnabled)
            {
                return;
            }

            var player = _playerManager.GetSessionByChannel(message.MsgChannel);
            if (player.AttachedEntity == null)
            {
                return;
            }

            var allAccess = IoCManager.Resolve<IPrototypeManager>()
                .EnumeratePrototypes<AccessLevelPrototype>()
                .Select(p => p.ID).ToArray();

            if (player.AttachedEntity.TryGetComponent(out InventoryComponent? inv)
                && inv.TryGetSlotItem(Slots.IDCARD, out ItemComponent? wornItem))
            {
                if (wornItem.Owner.HasComponent<AccessComponent>())
                {
                    UpgradeId(wornItem.Owner);
                }
                else if (wornItem.Owner.TryGetComponent(out PDAComponent? pda))
                {
                    if (pda.ContainedID == null)
                    {
                        pda.InsertIdCard(CreateFreshId().GetComponent<IdCardComponent>());
                    }
                    else
                    {
                        UpgradeId(pda.ContainedID.Owner);
                    }
                }
            }
            else if (player.AttachedEntity.TryGetComponent<HandsComponent>(out var hands))
            {
                var card = CreateFreshId();
                if (!player.AttachedEntity.TryGetComponent(out inv) || !inv.Equip(Slots.IDCARD, card))
                {
                    hands.PutInHandOrDrop(card.GetComponent<ItemComponent>());
                }
            }

            void UpgradeId(IEntity id)
            {
                var access = id.GetComponent<AccessComponent>();
                access.SetTags(allAccess);

                if (id.TryGetComponent(out SpriteComponent? sprite))
                {
                    sprite.LayerSetState(0, "gold");
                }
            }

            IEntity CreateFreshId()
            {
                var card = _entityManager.SpawnEntity("CaptainIDCard", player.AttachedEntity.Transform.Coordinates);
                UpgradeId(card);

                card.GetComponent<IdCardComponent>().FullName = player.AttachedEntity.Name;
                return card;
            }
        }

        private void SandboxGiveAghostReceived(MsgSandboxGiveAghost message)
        {
            if (!IsSandboxEnabled)
            {
                return;
            }

            var player = _playerManager.GetSessionByChannel(message.MsgChannel);

            _host.ExecuteCommand(player, _conGroupController.CanCommand(player, "aghost") ? "aghost" : "ghost");
        }

        private void SandboxSuicideReceived(MsgSandboxSuicide message)
        {
            if (!IsSandboxEnabled)
            {
                return;
            }

            var player = _playerManager.GetSessionByChannel(message.MsgChannel);
            _host.ExecuteCommand(player, "suicide");
        }

        private void UpdateSandboxStatusForAll()
        {
            var msg = _netManager.CreateNetMessage<MsgSandboxStatus>();
            msg.SandboxAllowed = IsSandboxEnabled;
            _netManager.ServerSendToAll(msg);
        }
    }
}
