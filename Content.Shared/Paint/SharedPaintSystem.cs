using Content.Shared.Popups;
using Content.Shared.Interaction;
using Content.Shared.Chemistry.EntitySystems;
using Robust.Shared.Timing;
using Robust.Shared.Audio.Systems;
using Content.Shared.Humanoid;
using Content.Shared.Decals;

namespace Content.Shared.Paint;

/// <summary>
/// Colors target and consumes reagent on each color success.
/// </summary>
public abstract class SharedPaintSystem : EntitySystem
{

    public override void Initialize()
    {
        base.Initialize();

    }

    public virtual void UpdateAppearance(EntityUid uid, PaintedComponent? component = null)
    {
    }
}
