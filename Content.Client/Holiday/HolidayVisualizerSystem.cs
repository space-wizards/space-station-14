using Content.Shared.Holiday;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Shared.Serialization.TypeSerializers.Implementations;

namespace Content.Client.Holiday;

/// <summary>
/// This handles...
/// </summary>
public sealed class HolidayVisualizerSystem : VisualizerSystem<HolidayVisualsComponent>
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
    }

    protected override void OnAppearanceChange(EntityUid uid, HolidayVisualsComponent comp, ref AppearanceChangeEvent args)
    {

    }
}
