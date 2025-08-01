using Content.Shared.Lock;

namespace Content.Server.SecretLocks;

public sealed class VoiceTriggerLockSystem : EntitySystem
{
    [Dependency] private readonly LockSystem _lock = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VoiceTriggerLockComponent, ComponentInit>(OnComponentInit);

        SubscribeLocalEvent<VoiceTriggerLockComponent, VoiceTriggeredEvent>(OnVoiceTriggered);
        SubscribeLocalEvent<VoiceTriggerLockComponent, TryShowVoiceTriggerVerbs>(OnShowVoiceTriggerVerbs);
        SubscribeLocalEvent<VoiceTriggerLockComponent, TryShowVoiceTriggerExamine>(OnShowVoiceTriggerExamine);
    }

    private void OnComponentInit(Entity<VoiceTriggerLockComponent> ent, ref ComponentInit args)
    {
        if (!HasComp<LockComponent>(ent))
            Log.Warning($"Entity with VoiceTriggerLockComponent {ent.Owner} has no lock component.");
    }

    private void OnVoiceTriggered(Entity<VoiceTriggerLockComponent> ent, ref VoiceTriggeredEvent args)
    {
        _lock.ToggleLock(ent);
    }

    private void OnShowVoiceTriggerVerbs(Entity<VoiceTriggerLockComponent> ent, ref TryShowVoiceTriggerVerbs args)
    {
        args.Canceled = _lock.IsLocked(ent.Owner);
    }

    private void OnShowVoiceTriggerExamine(Entity<VoiceTriggerLockComponent> ent, ref TryShowVoiceTriggerExamine args)
    {
        args.Canceled = _lock.IsLocked(ent.Owner);
    }
}
