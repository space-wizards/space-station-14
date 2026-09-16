using Content.Server.Administration;
using Content.Server.Administration.Managers;
using Content.Server.Chat.Managers;
using Content.Server.Tools;
using Content.Shared.Database;
using Content.Shared.Emag.Systems;
using Content.Shared.Fax;
using Content.Shared.Fax.Components;
using Content.Shared.Interaction;
using Content.Shared.Tools;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Fax;

public sealed partial class ServerFaxSystem : FaxSystem
{
    [Dependency] private IChatManager _chat = default!;
    [Dependency] private IAdminManager _adminManager = default!;
    [Dependency] private QuickDialogSystem _quickDialog = default!;
    [Dependency] private ToolSystem _toolSystem = default!;

    private static readonly ProtoId<ToolQualityPrototype> ScrewingQuality = "Screwing";

    private static readonly SoundSpecifier AdminAlert = new SoundPathSpecifier("/Audio/Machines/high_tech_confirm.ogg");

    // I'm not gonna refactor this till it can be moved to shared, and it can't be moved to shared until Quick dialogue is kill.
    [SubscribeLocalEvent]
    private void OnInteractUsing(Entity<FaxMachineComponent> entity, ref InteractUsingEvent args)
    {
        var user = args.User;
        if (args.Handled ||
            !TryComp<ActorComponent>(user, out var actor) ||
            !_toolSystem.HasQuality(args.Used, ScrewingQuality)) // Screwing because Pulsing already used by device linking
            return;

        _quickDialog.OpenDialog(actor.PlayerSession,
            Loc.GetString("fax-machine-dialog-rename"),
            Loc.GetString("fax-machine-dialog-field-name"),
            (string newName) =>
            {
                if (entity.Comp.FaxName == newName)
                    return;

                if (newName.Length > 20)
                {
                    Popup.PopupEntity(Loc.GetString("fax-machine-popup-name-long"), entity);
                    return;
                }

                if (entity.Comp.KnownFaxes.ContainsValue(newName) && !Emag.CheckFlag(entity, EmagType.Interaction)) // Allow existing names if emagged for fun
                {
                    Popup.PopupEntity(Loc.GetString("fax-machine-popup-name-exist"), entity);
                    return;
                }

                AdminLogger.Add(LogType.Action,
                    LogImpact.Low,
                    $"{ToPrettyString(user):user} renamed {ToPrettyString(entity):tool} from \"{entity.Comp.FaxName}\" to \"{newName}\"");
                entity.Comp.FaxName = newName;
                Popup.PopupEntity(Loc.GetString("fax-machine-popup-name-set"), entity);
                UpdateUserInterface(entity);
            });

        args.Handled = true;
    }

    protected override void NotifyAdmins(string faxName)
    {
        // Because why would a Shared system EVER NEED TO SEND AN ADMIN ANNOUNCEMENT???????????????????????????
        _chat.SendAdminAnnouncement(Loc.GetString("fax-machine-chat-notify", ("fax", faxName)));
        AudioSystem.PlayGlobal(AdminAlert, Filter.Empty().AddPlayers(_adminManager.ActiveAdmins), false, AudioParams.Default.AddVolume(-8f));
    }
}
