using Content.Shared.Shuttles.Components;

namespace Content.Shared.Shuttles.Systems;

public abstract class SharedRadarConsoleSystem : EntitySystem
{
    public const float DefaultMinRange = 64f;
    public const float DefaultMaxRange = 256f;

    protected virtual void UpdateState(EntityUid uid, RadarConsoleComponent component)
    {
    }

    public void SetRange(EntityUid uid, float value, RadarConsoleComponent component)
    {
        if (component.MaxRange.Equals(value))
            return;

        component.MaxRange = value;
        Dirty(uid, component);
        UpdateState(uid, component);
    }
}
