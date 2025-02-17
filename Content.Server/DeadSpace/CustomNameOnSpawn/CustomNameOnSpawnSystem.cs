using Content.Server.Administration;
using Content.Shared.Preferences;
using Content.Server.Chat.Managers;
using Content.Shared.GameTicking;

namespace Content.Server.DeadSpace.CustomNameOnSpawn;

public sealed class CustomNameOnSpawnSystem : EntitySystem
{
    [Dependency] private readonly MetaDataSystem _metaSystem = default!;
    [Dependency] private readonly QuickDialogSystem _quickDialog = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<CustomNameOnSpawnComponent, PlayerSpawnCompleteEvent>(OnPlayerSpawned);
    }

    private void OnPlayerSpawned(EntityUid ent, CustomNameOnSpawnComponent component, PlayerSpawnCompleteEvent args)
    {
        ShowNameChangeMenu(ent, component, args);
    }

    private void ShowNameChangeMenu(EntityUid ent, CustomNameOnSpawnComponent component, PlayerSpawnCompleteEvent args)
    {
        _quickDialog.OpenDialog(args.Player,
            Loc.GetString("custom-name-on-start-dialog-title"),
            Loc.GetString("custom-name-on-start-dialog-newname-text"),
            (string newName) =>
            {
                if (newName.Length <= 3)
                {
                    _chatManager.DispatchServerMessage(
                        args.Player,
                        Loc.GetString("custom-name-on-start-too-short"),
                        true);
                    ShowNameChangeMenu(ent, component, args);
                    return;
                }
                if (newName.Length >= HumanoidCharacterProfile.MaxNameLength)
                {
                    _chatManager.DispatchServerMessage(
                        args.Player,
                        Loc.GetString("custom-name-on-start-too-long"),
                        true);
                    ShowNameChangeMenu(ent, component, args);
                    return;
                }

                _metaSystem.SetEntityName(ent, newName);
            });
    }
}
