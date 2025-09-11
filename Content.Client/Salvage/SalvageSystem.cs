using Content.Client.Audio;
using Content.Shared.Salvage;
using Content.Shared.Salvage.Expeditions;
using Robust.Client.Player;
using Robust.Shared.GameStates;

namespace Content.Client.Salvage;

public sealed class SalvageSystem : SharedSalvageSystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly ContentAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlayAmbientMusicEvent>(OnPlayAmbientMusic);
        SubscribeLocalEvent<SalvageExpeditionComponent, ComponentHandleState>(OnExpeditionHandleState);
    }

    private void OnExpeditionHandleState(EntityUid uid, SalvageExpeditionComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not SalvageExpeditionComponentState state)
            return;

        component.Stage = state.Stage;

        if (component.Stage >= ExpeditionStage.MusicCountdown)
        {
            _audio.DisableAmbientMusic();
        }
    }

    private void OnPlayAmbientMusic(ref PlayAmbientMusicEvent ev)
    {
        if (ev.Cancelled)
            return;

        var player = _playerManager.LocalEntity;

        if (!TryComp(player, out TransformComponent? xform) ||
            !TryComp<SalvageExpeditionComponent>(xform.MapUid, out var expedition) ||
            expedition.Stage < ExpeditionStage.MusicCountdown)
        {
            return;
        }

        ev.Cancelled = true;
    }
}
