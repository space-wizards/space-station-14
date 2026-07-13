using System.Numerics;
using Content.Client.Administration.Managers;
using Content.Client.Lobby;
using Content.Shared.Paper;
using Content.Shared.Signature;

namespace Content.Client.Signature;

public sealed partial class SignatureSystem : SharedSignatureSystem
{
    [Dependency] private IClientPreferencesManager _preferences = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private IClientAdminManager _admin = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PaperComponent, ApplySavedSignature>(OnApplySavedSignature);
        SubscribeNetworkEvent<SendSignatureToAdminEvent>(OnSignature);
    }

    private void OnApplySavedSignature(Entity<PaperComponent> ent, ref ApplySavedSignature args)
    {
        var profile = _preferences.Preferences?.SelectedCharacter;

        if (profile?.SignatureData == null)
            return;

        var state = new UpdateSignatureDataState(profile.SignatureData);
        _ui.SetUiState(ent.Owner, PaperComponent.PaperUiKey.Key, state);
    }

    private void OnSignature(SendSignatureToAdminEvent ev)
    {
        if (!_admin.IsAdmin())
            return;

        var canvasSize = new Vector2(ev.Data.Width, ev.Data.Height);
        var window = new SignatureWindow(canvasSize);
        window.Signature.SetSignature(ev.Data);

        window.OpenCentered();
    }
}
