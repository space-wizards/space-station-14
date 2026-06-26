using Content.Shared.Instruments;
using Content.Shared.Toggleable;
using Robust.Shared.GameObjects;

namespace Content.Server.Instruments;

/// <summary>
/// Makes instruments toggle their appearance depending on whether they're playing music or not.
/// </summary>
/// <see cref="ToggleableVisuals"/>
public sealed class InstrumentToggleVisualsSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<InstrumentToggleVisualsComponent, InstrumentComponent>();
        while (query.MoveNext(out var uid, out var instrumentVisuals, out var instrument))
        {
            _appearance.SetData(uid, ToggleableVisuals.Enabled, instrument.Playing);
        }
    }
}
