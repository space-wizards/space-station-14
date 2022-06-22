using Content.Shared.Movement.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.Movement.Systems;

public abstract class JetpackSystem : EntitySystem
{
    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        _gasTank?.RemoveAirVolume(VolumeUsage);
        foreach (var comp in EntityQuery<JetpackComponent>())
        {
            if (!comp.Enabled) continue;
        }
    }

    public void SetEnabled(SharedJetpackComponent component, bool enabled)
    {
        if (component.Enabled == enabled) return;

        component.Enabled = enabled;
    }

    [Serializable, NetSerializable]
    protected sealed class JetpackComponentState : ComponentState
    {
        public bool Enabled;
    }
}
