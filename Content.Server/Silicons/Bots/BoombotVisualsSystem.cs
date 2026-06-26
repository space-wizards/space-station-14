using Content.Server.Instruments;
using Content.Shared.Silicons.Bots;
using Content.Shared.Toggleable;
using Robust.Shared.GameObjects;

namespace Content.Server.Silicons.Bots;

/// <summary>
/// Makes boombots update their visuals depending on whether they're playing music or not.
/// </summary>
public sealed class BoombotInstrumentVisualizerSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<BoombotVisualsComponent, InstrumentComponent>();
        while (query.MoveNext(out var uid, out var boom, out var instrument))
        {
            _appearance.SetData(uid, ToggleableVisuals.Enabled, instrument.Playing);
        }
    }
}
