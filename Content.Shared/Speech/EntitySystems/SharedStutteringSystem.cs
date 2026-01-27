using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Shared.Speech.EntitySystems;

public abstract class SharedStutteringSystem : EntitySystem
{
    public static readonly EntProtoId Stuttering = "StatusEffectStutter";

    [Dependency] protected readonly StatusEffectsSystem Status = default!;

    // For code in shared... I imagine we ain't getting accent prediction anytime soon so let's not bother.
    public virtual void DoStutter(EntityUid uid, TimeSpan time, bool refresh)
    {
    }

    public virtual void DoRemoveStutterTime(EntityUid uid, TimeSpan timeRemoved)
    {
    }

    public virtual void DoRemoveStutter(EntityUid uid)
    {
    }
}
