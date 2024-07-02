using Content.Shared.Administration.Logs;
using Content.Shared.Popups;

namespace Content.Shared.VoiceRecorder;

public abstract partial class SharedVoiceRecordedSystem : EntitySystem
{
    [Dependency] protected readonly SharedAppearanceSystem AppearanceSystem = default!;
    protected void UpdateAppearance(EntityUid uid, VoiceRecorderComponent? component = null, AppearanceComponent? appearance = null)
    {
        if (!Resolve(uid, ref component, ref appearance, false))
            return;

        AppearanceSystem.SetData(uid, VoiceRecorderVisuals.IsRecording, component.IsRecording, appearance);
    }
}
