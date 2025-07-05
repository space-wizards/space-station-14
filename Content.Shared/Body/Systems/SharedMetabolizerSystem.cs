using Content.Shared.Body.Events;

namespace Content.Shared.Body.Systems;

public sealed class SharedMetabolizerSystem : EntitySystem
{
    public void UpdateMetabolicMultiplier(EntityUid uid)
    {
        var getEv = new GetMetabolicMultiplierEvent();
        RaiseLocalEvent(uid, ref getEv);

        var applyEv = new ApplyMetabolicMultiplierEvent(getEv.Multiplier);
        RaiseLocalEvent(uid, ref applyEv);
    }
}
