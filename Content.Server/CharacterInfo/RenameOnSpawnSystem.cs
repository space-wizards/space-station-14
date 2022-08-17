using Content.Server.Access.Systems;
using Content.Server.Administration;
using Content.Server.Administration.Systems;
using Content.Server.Mind.Components;
using Content.Server.PDA;
using Content.Server.Station.Systems;
using Content.Server.StationRecords;
using Content.Shared.Access.Components;
using Content.Shared.PDA;
using Content.Shared.StationRecords;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Enums;

namespace Content.Server.CharacterInfo
{
    public sealed class RenameOnSpawnSystem : EntitySystem
    {
        [Dependency] private readonly IdCardSystem _idCardSystem = default!;
        [Dependency] private readonly QuickDialogSystem _quickDialog = default!;
        [Dependency] private readonly AdminSystem _adminSystem = default!;
        [Dependency] private readonly PDASystem _pdaSystem = default!;
        [Dependency] private readonly StationRecordsSystem _stationRecords = default!;
        [Dependency] private readonly StationSystem _stationSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<RenameOnSpawnComponent, PlayerAttachedEvent>(OnPlayerAttached);
        }

        private void OnPlayerAttached(EntityUid uid, RenameOnSpawnComponent component, PlayerAttachedEvent args)
        {
            ShowNameChangePopup(component, args.Player);
        }

        public void ShowNameChangePopup(RenameOnSpawnComponent component, IPlayerSession player)
        {
            //popup rename menu
            _quickDialog.OpenDialog(player, "Rename yourself", "Name", (string newName) =>
            {
                TryNameChange(component, newName, player);
            });
        }

        private void TryNameChange(RenameOnSpawnComponent component, string newName, IPlayerSession player)
        {
            var oldName = player.Name;

            if (oldName == newName)
                return;

            //check if new name is too long
            if (newName.Length > SharedIdCardConsoleComponent.MaxFullNameLength)
            {
                ShowNameChangePopup(component, player);
                return;
            }

            //get metadata component
            if (!TryComp<MetaDataComponent>(component.Owner, out var metaDataComponent))
                return;

            //Get ID card
            if (!_idCardSystem.TryFindIdCard(component.Owner, out var idCard))
                return;

            //Change name in station records
            var station = _stationSystem.GetOwningStation(component.Owner);
            if (station == null)
                return;
            if (!TryComp<StationRecordKeyStorageComponent>(idCard.Owner, out var stationRecordKeyStorageComponent) || stationRecordKeyStorageComponent.Key == null)
                return;
            if (!_stationRecords.TryGetRecord(station.Value, stationRecordKeyStorageComponent.Key.Value, out GeneralStationRecord? record))
                return;
            record.Name = newName;


            //change name in metadata
            metaDataComponent.EntityName = newName;

            //change name in mind
            if (TryComp<MindComponent>(component.Owner, out var mindComponent) && mindComponent.Mind != null)
            {
                mindComponent.Mind.CharacterName = newName;
            }

            //change name on ID
            _idCardSystem.TryChangeFullName(idCard.Owner, newName, idCard);

            //change name on PDA
            foreach (var pdaComponent in EntityQuery<PDAComponent>())
            {
                if (pdaComponent.OwnerName != oldName)
                    continue;
                _pdaSystem.SetOwner(pdaComponent, newName);
            }

            //update name on admin list
            _adminSystem.UpdatePlayerList(player);

            //remove component since the deed is done
            RemComp<RenameOnSpawnComponent>(component.Owner);

        }
    }
}
