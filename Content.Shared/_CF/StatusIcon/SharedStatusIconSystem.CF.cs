using Content.Shared.StatusIcon.Components;

namespace Content.Shared.StatusIcon;

public abstract partial class SharedStatusIconSystem
{
    /// <summary>
    /// Gives a creature a status-icon anchor so the flanked mark can render.
    /// Returns true only when this call added the component.
    /// </summary>
    public bool CFTryAddStatusIcon(EntityUid uid)
    {
        if (HasComp<StatusIconComponent>(uid))
            return false;

        var comp = EnsureComp<StatusIconComponent>(uid);
        comp.Bounds = new Box2(-0.5f, -0.5f, 0.5f, 0.5f);
        Dirty(uid, comp);
        return true;
    }

    public void CFRemoveStatusIcon(EntityUid uid)
    {
        RemComp<StatusIconComponent>(uid);
    }
}
