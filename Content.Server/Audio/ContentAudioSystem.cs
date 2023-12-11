using Content.Server.GameTicking.Events;
using Content.Shared.Audio;
using Content.Shared.GameTicking;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Audio;

public sealed class ContentAudioSystem : SharedContentAudioSystem
{
    [Dependency] private readonly AudioSystem _serverAudio = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundCleanup);
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStart);
        _protoManager.PrototypesReloaded += OnProtoReload;
    }

    private void OnRoundCleanup(RoundRestartCleanupEvent ev)
    {
        SilenceAudio();
    }

    private void OnProtoReload(PrototypesReloadedEventArgs obj)
    {
        if (!obj.ByType.ContainsKey(typeof(AudioPresetPrototype)))
            return;

        _serverAudio.ReloadPresets();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _protoManager.PrototypesReloaded -= OnProtoReload;
    }

    private void OnRoundStart(RoundStartingEvent ev)
    {
        // On cleanup all entities get purged so need to ensure audio presets are still loaded
        // yeah it's whacky af.
        _serverAudio.ReloadPresets();
    }
}
