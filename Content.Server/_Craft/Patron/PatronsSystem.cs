using System.Threading.Tasks;

using Content.Shared.Administration;
using Content.Shared.Popups;
using Content.Shared.Item;
using Content.Shared.Inventory;

using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Server.Mind.Components;
using Content.Server.Storage.EntitySystems;
using Content.Server.Administration.Managers;

using Robust.Shared.Asynchronous;
using Robust.Shared.Prototypes;

using Robust.Server.Player;

namespace Content.Server.Patron
{
    public sealed class PatronSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _protoMan = default!;
        [Dependency] private readonly ITaskManager _taskManager = default!;
        [Dependency] private readonly IServerDbManager _dbMan = default!;
        [Dependency] private readonly IAdminManager _adminMan = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;
        [Dependency] private readonly StorageSystem _storeSys = default!;
        [Dependency] private readonly InventorySystem _invSys = default!;


        private ISawmill _sawmill = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
            SubscribeLocalEvent<PatronItemComponent,GettingPickedUpAttemptEvent>(OnItemPickTry);
        }
        private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent ev)
        {
            _taskManager.BlockWaitOnTask(GivePatronItems(ev.Player, ev.Mob));
        }

        private void OnItemPickTry(EntityUid uid, PatronItemComponent comp, GettingPickedUpAttemptEvent ev)
        {
            if (HasComp<MindComponent>(ev.User))
            {
                var mind = Comp<MindComponent>(ev.User).Mind;
                if (mind is null || mind.Session is null)
                {
                    ev.Cancel();
                    return;
                }
                var session = mind.Session;
                if (session != comp.Patron && (!_adminMan.IsAdmin(session) || !_adminMan.HasAdminFlag(session, AdminFlags.Host)))
                {
                    ev.Cancel();
                    _popup.PopupCursor(Loc.GetString("patronitem-denypickup"), ev.User);
                    return;
                }
                else
                    return;
            }
            ev.Cancel();
            return;
        }

        private async Task GivePatronItems(IPlayerSession ply, EntityUid ent)
        {
            var guid = ply.UserId.UserId;
            if (!HasComp<InventoryComponent>(ent)) return;
            if (!_invSys.TryGetSlotEntity(ent, "back", out var trgTryInv))
            {
#if DEBUG
                _sawmill.Warning("[Patronlist] Не в состоянии выдать предметы - У игрока [" + ply.Name + "] энтити [" + ent.ToString() + "] не найден рюкзак - это за кого его заспавнило то?");
#endif
                return;
            }
            if (trgTryInv is null) return;
            EntityUid trgInv = trgTryInv.Value;

            if (!await _dbMan.IsInPatronlistAsync(guid)) return;
            if (!ent.IsValid() || !trgInv.IsValid()) return;

            var items = await _dbMan.GetPatronItemsAsync(guid);
            var coords = Transform(ent).MapPosition;

            foreach (var item in items)
            {
                if (!_protoMan.HasIndex<EntityPrototype>(item))
                    continue;
                var itemEnt = Spawn(item, coords);
                EnsureComp<ItemComponent>(itemEnt);
                var patcomp = EnsureComp<PatronItemComponent>(itemEnt);
                patcomp.Patron = ply;
                _storeSys.Insert(trgInv, itemEnt, playSound: false);
            }
        }
    }
}
