using Content.Shared.SecretLocks;

namespace Content.Server.SecretLocks;

public sealed class VoiceTriggerLockSystem : SharedVoiceTriggerLockSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VoiceTriggerLockComponent, VoiceTriggeredEvent>(OnVoiceTriggered);
        SubscribeLocalEvent<VoiceTriggerLockComponent, TryShowVoiceTriggerVerbs>(OnShowVoiceTriggerVerbs);
        SubscribeLocalEvent<VoiceTriggerLockComponent, TryShowVoiceTriggerExamine>(OnShowVoiceTriggerExamine);
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
