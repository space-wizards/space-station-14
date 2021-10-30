using System.Linq;
using Content.Server.Access.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Server.GameTicking;
using Content.Server.Hands.Components;
using Content.Server.Inventory.Components;
using Content.Server.Items;
using Content.Server.PDA;
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
using static Content.Shared.Inventory.EquipmentSlotDefines;
using Content.Server.Access.Systems;

namespace Content.Server.Sandbox
{
    internal sealed class SandboxManager : SharedSandboxManager, ISandboxManager, IEntityEventSubscriber
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IServerNetManager _netManager = default!;
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
            _netManager.RegisterNetMessage<MsgSandboxStatus>();
            _netManager.RegisterNetMessage<MsgSandboxRespawn>(SandboxRespawnReceived);
            _netManager.RegisterNetMessage<MsgSandboxGiveAccess>(SandboxGiveAccessReceived);
            _netManager.RegisterNetMessage<MsgSandboxGiveAghost>(SandboxGiveAghostReceived);
            _netManager.RegisterNetMessage<MsgSandboxSuicide>(SandboxSuicideReceived);

            _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
            _entityManager.EventBus.SubscribeEvent<GameRunLevelChangedEvent>(EventSource.Local, this, GameTickerOnOnRunLevelChanged);

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

        private void GameTickerOnOnRunLevelChanged(GameRunLevelChangedEvent obj)
        {
            // Automatically clear sandbox state when round resets.
            if (obj.New == GameRunLevel.PreRoundLobby)
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
            EntitySystem.Get<GameTicker>().Respawn(player);
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
                        var newID = CreateFreshId();
                        if (pda.Owner.TryGetComponent(out SharedItemSlotsComponent? itemSlots))
                        {
                            _entityManager.EntitySysManager.GetEntitySystem<SharedItemSlotsSystem>().
                                TryInsertContent(itemSlots, newID, pda.IdSlot);
                        }
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
                var accessSystem = EntitySystem.Get<AccessSystem>();
                accessSystem.TrySetTags(id.Uid, allAccess);

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
