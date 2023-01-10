using Content.Shared.FixedPoint;
using Content.Shared.Medical.Circulation.Components;
using Robust.Shared.Utility;

namespace Content.Shared.Medical.Circulation;


[ByRefEvent]
public record struct CirculationReagentsUpdated(CirculationComponent Component);

[ByRefEvent]
public struct CirculationTickEvent
{
    public CirculationTickEvent(CirculationComponent component, Dictionary<string, FixedPoint2> volumeChanges)
    {
        this.Component = component;
        this._volumeChanges = volumeChanges;
    }

    public CirculationComponent Component { get; set; }
    private Dictionary<string, FixedPoint2> _volumeChanges { get; set; }
    public IReadOnlyDictionary<string, FixedPoint2> VolumeChanges => _volumeChanges;

    public void AdjustVolume(string reagentId, FixedPoint2 volumeChange)
    {
        if (_volumeChanges.TryAdd(reagentId, volumeChange))
            return;
        _volumeChanges[reagentId] += volumeChange;
    }

    public void SetVolume(string reagentId, FixedPoint2 newVolume)
    {
        if (_volumeChanges.TryAdd(reagentId, newVolume))
            return;
        _volumeChanges[reagentId] = newVolume;
    }
}
