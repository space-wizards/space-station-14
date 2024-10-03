using Content.Shared.Containers.ItemSlots;
using Content.Shared.Examine;
using Content.Shared.Singularity.Components;

namespace Content.Shared.Singularity.EntitySystems;

public abstract class SharedContainmentAlarmSystem : EntitySystem
{

    public const string ContainerName = "alarmSlot";
    public override void Initialize()
    {
        SubscribeLocalEvent<ContainmentAlarmComponent, ExaminedEvent>(OnExamine);

        SubscribeLocalEvent<ContainmentAlarmHolderComponent, ItemSlotEjectAttemptEvent>(OnTryEject);
    }

    private void OnExamine(Entity<ContainmentAlarmComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("comp-containment-alert-field-alarm"));
    }

    private void OnTryEject(Entity<ContainmentAlarmHolderComponent> ent, ref ItemSlotEjectAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!TryComp<ContainmentFieldGeneratorComponent>(ent, out var gen))
            return;

        if (gen.Enabled)
            args.Cancelled = true;
    }
}
