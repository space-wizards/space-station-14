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
using Content.Shared.Humanoid;
using Content.Shared.Interaction.Events;
using Robust.Server.GameObjects;
using Content.Shared.Patron;
using Content.Server.Speech.Components;

namespace Content.Server.Patron
{
    public sealed class PatronSystem : SharedPatronSystem
    {
        [Dependency] private readonly IPrototypeManager _protoMan = default!;
        [Dependency] private readonly ITaskManager _taskManager = default!;
        [Dependency] private readonly IServerDbManager _dbMan = default!;
        [Dependency] private readonly IAdminManager _adminMan = default!;
        [Dependency] private readonly IEntityManager _entMan = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;
        [Dependency] private readonly StorageSystem _storeSys = default!;
        [Dependency] private readonly InventorySystem _invSys = default!;

        private ISawmill _sawmill = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
            SubscribeLocalEvent<PatronItemComponent,GettingPickedUpAttemptEvent>(OnItemPickTry);
            SubscribeLocalEvent<PatronEarsComponent, UseInHandEvent>(OnApplyEars);
        }
        private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent ev)
        {
            _taskManager.BlockWaitOnTask(GivePatronItems(ev.Player, ev.Mob));
        }

        private void OnItemPickTry(EntityUid uid, PatronItemComponent comp, GettingPickedUpAttemptEvent ev)
        {
            var user = ev.User;
            if (IsItemOwnedByEntity(comp, user, out var session) || (session is not null && _adminMan.IsAdmin(session) && _adminMan.HasAdminFlag(session, AdminFlags.Host)))
                return;
            ev.Cancel();
            _popup.PopupCursor(Loc.GetString("patronitem-denypickup"), user);
            return;
        }

        private void OnApplyEars(EntityUid uid, PatronEarsComponent comp, UseInHandEvent ev)
        {
            var user = ev.User;
            if (TryComp<PatronItemComponent>(uid, out var patronComp))
                if (!IsItemOwnedByEntity(patronComp, user, out var session))
                    return;

            var curse = EnsureComp<OwOAccentComponent>(user);
            var visComp = EnsureComp<PatronEarsVisualizerComponent>(user);
            visComp.RsiPath = comp.RsiPath;
            Dirty(visComp);
            _entMan.DeleteEntity(uid);
        }

        private bool IsItemOwnedByEntity(PatronItemComponent comp, EntityUid uid, out IPlayerSession? session)
        {
            session = null;
            if (!HasComp<MindComponent>(uid))
                return false;
            var mind = Comp<MindComponent>(uid).Mind;
            if (mind is null || mind.Session is null)
                return false;
            session = mind.Session;
            return session == comp.Patron;
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
