using System.Text.Json;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Popups;
using Content.Shared.Database;
using Content.Shared.Paper;
using Content.Shared.Signature;

namespace Content.Server.Signature;

public sealed partial class SignatureSystem : SharedSignatureSystem
{
    [Dependency] private IAdminLogManager _adminLog = default!;
    [Dependency] private IAdminManager _adminManager = default!;
    [Dependency] private PopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<RequestSignatureAdminMessage>(OnRequestSignatureAdmin);
    }

    private async void OnRequestSignatureAdmin(RequestSignatureAdminMessage args, EntitySessionEventArgs ev)
    {
        var userEnt = ev.SenderSession.AttachedEntity;
        if (userEnt == null)
            return;

        if (!_adminManager.IsAdmin(ev.SenderSession, true))
            return;

        var log = await _adminLog.GetJsonByLogId(args.LogId, args.Time);
        if (log == null)
        {
            _popup.PopupCursor(Loc.GetString("admin-logs-signature-popup-no-record-in-db"), userEnt.Value);
            return;
        }

        SignatureData? signature = null;

        var root = log.RootElement;
        foreach (var child in root.EnumerateObject())
        {
            if (child.Value.ValueKind != JsonValueKind.Object)
                continue;

            var obj = child.Value;

            if (!obj.TryGetProperty("serialized", out var serProp))
                continue;

            var serialized = serProp.GetString();
            if (string.IsNullOrEmpty(serialized))
                continue;

            var sig = SignatureSerializer.Deserialize(serialized);
            if (sig == null)
                continue;

            signature = sig;
        }

        if (signature == null)
        {
            _popup.PopupCursor(Loc.GetString("admin-logs-signature-popup-cant-find-signature"), userEnt.Value);
            return;
        }

        var req = new SendSignatureToAdminEvent(signature);
        RaiseNetworkEvent(req, ev.SenderSession);
    }

    protected override void AfterSubmitSignature(Entity<PaperComponent, SignatureComponent> ent, ref SignatureSubmitMessage args, bool changedSignature)
    {
        base.AfterSubmitSignature(ent, ref args, changedSignature);

        var verboseChangedSignature = changedSignature ? "changed signature" : "written without changing signature";

        if (ent.Comp2.Data is not null)
            _adminLog.Add(LogType.Chat, LogImpact.Medium, $"{ToPrettyString(args.Actor):user} has {verboseChangedSignature} {new SignatureLogData(ent.Comp2.Data)} on {ToPrettyString(ent):target}");
    }
}
