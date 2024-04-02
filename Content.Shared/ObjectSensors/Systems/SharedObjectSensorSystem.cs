using Content.Shared.ObjectSensors.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.StepTrigger.Components;
using Content.Shared.Timing;
using Content.Shared.Toggleable;
using Content.Shared.Tools.Systems;
using Robust.Shared.Audio.Systems;

namespace Content.Shared.ObjectSensors.Systems;

public abstract class SharedObjectSensorSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public readonly int ModeCount = Enum.GetValues(typeof(ObjectSensorMode)).Length;

    public override void Initialize()
    {
        base.Initialize();

    }



    public int GetTotalEntitites(Entity<ObjectSensorComponent> uid)
    {
        if (!TryComp(uid, out StepTriggerComponent? stepTrigger))
            return 0;

        var contacting = stepTrigger.Colliding;
        var total = 0;

        foreach (var ent in contacting)
        {
            switch (uid.Comp.Mode)
            {
                case ObjectSensorMode.Living:
                    if (_mobState.IsAlive(ent))
                        total++;
                    break;
                case ObjectSensorMode.Items:
                    if (HasComp<ItemComponent>(ent))
                        total++;
                    break;
                case ObjectSensorMode.All:
                    total++;
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        return total;
    }
}

