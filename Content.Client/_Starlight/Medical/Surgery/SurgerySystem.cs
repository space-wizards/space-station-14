using Content.Shared.Starlight.Medical.Surgery;

namespace Content.Client._Starlight.Medical.Surgery;
// Based on the RMC14.
// https://github.com/RMC-14/RMC-14
public sealed class SurgerySystem : SharedSurgerySystem
{
    public event Action? OnRefresh;
    public override void Update(float frameTime)
    {
        _delayAccumulator += frameTime;
        if (_delayAccumulator > 1) {
            _delayAccumulator = 0;
            OnRefresh?.Invoke();
        }
    }
}
