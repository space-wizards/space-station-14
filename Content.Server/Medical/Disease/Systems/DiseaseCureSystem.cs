using Content.Shared.Medical.Disease;

namespace Content.Server.Medical.Disease.Systems;

public sealed partial class DiseaseCureSystem : EntitySystem
{
    /// <summary>
    /// Runtime per-step state stored in the system.
    /// </summary>
    internal sealed class CureState
    {
        public float Ticker;
    }

    private readonly Dictionary<(EntityUid, string, CureStep), CureState> _cureStates = new();

    internal CureState GetState(EntityUid uid, string diseaseId, CureStep step)
    {
        var key = (uid, diseaseId, step);
        if (!_cureStates.TryGetValue(key, out var state))
        {
            state = new CureState();
            _cureStates[key] = state;
        }
        return state;
    }
}
