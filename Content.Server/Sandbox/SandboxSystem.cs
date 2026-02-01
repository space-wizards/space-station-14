using System.Linq;
using Content.Server.GameTicking;
using Content.Server.Popups;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.CCVar;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.PDA;
using Content.Shared.Popups;
using Content.Shared.Sandbox;
using Robust.Server.Console;
using Robust.Server.Placement;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Network.Messages;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.Sandbox
{
    public sealed class SandboxSystem : SharedSandboxSystem
    {
        [Dependency] private readonly IConfigurationManager _cfgManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IPlacementManager _placementManager = default!;
        [Dependency] private readonly IConGroupController _conGroupController = default!;
        [Dependency] private readonly IServerConsoleHost _host = default!;
        [Dependency] private readonly SharedAccessSystem _access = default!;
        [Dependency] private readonly InventorySystem _inventory = default!;
        [Dependency] private readonly ItemSlotsSystem _slots = default!;
        [Dependency] private readonly GameTicker _ticker = default!;
        [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;

        private int _maxEntitySpawnsPerTimeFrame;
        private TimeSpan _entitySpawnWindow;

        private readonly Dictionary<NetUserId, Queue<TimeSpan>> _entitySpawnHistories = new();
        private readonly Dictionary<NetUserId, TimeSpan> _lastRateLimitHits = new();
        private readonly TimeSpan _spawnRateLimitHitCooldown = TimeSpan.FromSeconds(1);

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

        public override void Initialize()
        {
            base.Initialize();
            SubscribeNetworkEvent<MsgSandboxRespawn>(SandboxRespawnReceived);
            SubscribeNetworkEvent<MsgSandboxGiveAccess>(SandboxGiveAccessReceived);
            SubscribeNetworkEvent<MsgSandboxGiveAghost>(SandboxGiveAghostReceived);
            SubscribeNetworkEvent<MsgSandboxSuicide>(SandboxSuicideReceived);

            SubscribeLocalEvent<GameRunLevelChangedEvent>(GameTickerOnOnRunLevelChanged);

            _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;

            _placementManager.AllowPlacementFunc = CanPlace;

            Subs.CVar(_cfgManager,
                CCVars.SandboxMaxEntitySpawnsPerTimeFrame,
                value => _maxEntitySpawnsPerTimeFrame = value,
                true);
            Subs.CVar(_cfgManager,
                CCVars.SandboxEntitySpawnTimeFrameLengthSeconds,
                value => _entitySpawnWindow = TimeSpan.FromSeconds(value),
                true);
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _placementManager.AllowPlacementFunc = null;
            _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;
        }

        private bool CanPlace(MsgPlacement placement)
        {
            var channel = placement.MsgChannel;
            var player = _playerManager.GetSessionByChannel(channel);

            if (_conGroupController.CanAdminPlace(player))
                return true;

            if (!IsSandboxEnabled)
                return false;

            if (placement.PlaceType != PlacementManagerMessage.RequestPlacement)
                return true;

            if (placement.IsTile)
                return true;

            return TryConsumeEntitySpawn(player.UserId, player);
        }

        private bool TryConsumeEntitySpawn(NetUserId userId, ICommonSession player)
        {
            var now = _timing.CurTime;

            if (_entitySpawnWindow <= TimeSpan.Zero || _maxEntitySpawnsPerTimeFrame <= 0)
                return true;

            if (_lastRateLimitHits.TryGetValue(userId, out var lastHit)
                && now - lastHit < _spawnRateLimitHitCooldown)
            {
                return false;
            }

            if (!_entitySpawnHistories.TryGetValue(userId, out var recentSpawns))
            {
                recentSpawns = new Queue<TimeSpan>(_maxEntitySpawnsPerTimeFrame + 1);
                _entitySpawnHistories[userId] = recentSpawns;
            }

            while (recentSpawns.Count > 0 && now - recentSpawns.Peek() > _entitySpawnWindow)
            {
                recentSpawns.Dequeue();
            }

            if (recentSpawns.Count >= _maxEntitySpawnsPerTimeFrame)
            {
                var uid = player.AttachedEntity;

                _lastRateLimitHits[userId] = now;
                if (uid is not null)
                {
                    _popupSystem.PopupEntity(Loc.GetString("sandbox-spawn-rate-reached-popup"), uid.Value, uid.Value, PopupType.Medium);
                }

                return false;
            }

            recentSpawns.Enqueue(now);
            return true;
        }

        private void GameTickerOnOnRunLevelChanged(GameRunLevelChangedEvent obj)
        {
            // Automatically clear sandbox state when round resets.
            if (obj.New == GameRunLevel.PreRoundLobby)
            {
                IsSandboxEnabled = false;
                _entitySpawnHistories.Clear();
                _lastRateLimitHits.Clear();
            }
        }

        private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
        {
            if (e.NewStatus != SessionStatus.Connected || e.OldStatus != SessionStatus.Connecting)
                return;

            RaiseNetworkEvent(new MsgSandboxStatus { SandboxAllowed = IsSandboxEnabled }, e.Session.Channel);
        }

        private void SandboxRespawnReceived(MsgSandboxRespawn message, EntitySessionEventArgs args)
        {
            if (!IsSandboxEnabled)
                return;

            var player = _playerManager.GetSessionByChannel(args.SenderSession.Channel);
            if (player.AttachedEntity == null) return;

            _ticker.Respawn(player);
        }

        private void SandboxGiveAccessReceived(MsgSandboxGiveAccess message, EntitySessionEventArgs args)
        {
            if (!IsSandboxEnabled)
                return;

            var player = _playerManager.GetSessionByChannel(args.SenderSession.Channel);
            if (player.AttachedEntity is not { } attached)
            {
                return;
            }

            var allAccess = PrototypeManager
                .EnumeratePrototypes<AccessLevelPrototype>()
                .Select(p => new ProtoId<AccessLevelPrototype>(p.ID)).ToList();

            if (_inventory.TryGetSlotEntity(attached, "id", out var slotEntity))
            {
                if (HasComp<AccessComponent>(slotEntity))
                {
                    UpgradeId(slotEntity.Value);
                }
                else if (TryComp<PdaComponent>(slotEntity, out var pda))
                {
                    if (pda.ContainedId is null)
                    {
                        var newID = CreateFreshId();
                        if (TryComp<ItemSlotsComponent>(slotEntity, out var itemSlots))
                        {
                            _slots.TryInsert(slotEntity.Value, pda.IdSlot, newID, null);
                        }
                    }
                    else
                    {
                        UpgradeId(pda.ContainedId!.Value);
                    }
                }
            }
            else if (TryComp<HandsComponent>(attached, out var hands))
            {
                var card = CreateFreshId();
                if (!_inventory.TryEquip(attached, card, "id", true, true))
                {
                    _handsSystem.PickupOrDrop(attached, card, handsComp: hands);
                }
            }

            void UpgradeId(EntityUid id)
            {
                _access.TrySetTags(id, allAccess);
            }

            EntityUid CreateFreshId()
            {
                var card = Spawn("CaptainIDCard", Transform(attached).Coordinates);
                UpgradeId(card);

                Comp<IdCardComponent>(card).FullName = MetaData(attached).EntityName;
                return card;
            }
        }

        private void SandboxGiveAghostReceived(MsgSandboxGiveAghost message, EntitySessionEventArgs args)
        {
            if (!IsSandboxEnabled)
                return;

            var player = _playerManager.GetSessionByChannel(args.SenderSession.Channel);

            _host.ExecuteCommand(player, _conGroupController.CanCommand(player, "aghost") ? "aghost" : "ghost");
        }

        private void SandboxSuicideReceived(MsgSandboxSuicide message, EntitySessionEventArgs args)
        {
            if (!IsSandboxEnabled)
                return;

            var player = _playerManager.GetSessionByChannel(args.SenderSession.Channel);
            _host.ExecuteCommand(player, "suicide");
        }

        private void UpdateSandboxStatusForAll()
        {
            RaiseNetworkEvent(new MsgSandboxStatus { SandboxAllowed = IsSandboxEnabled });
        }
    }
}
