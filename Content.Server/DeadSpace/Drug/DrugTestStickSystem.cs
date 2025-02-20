using Content.Server.DeadSpace.Drug.Components;
using Content.Shared.Interaction;
using Content.Shared.Nutrition.Components;
using Content.Shared.Forensics.Components;
using Content.Shared.DeadSpace.Drug.Components;
using Content.Shared.Popups;

namespace Content.Server.DeadSpace.Drug;

public sealed class DrugTestStickSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DrugTestStickComponent, AfterInteractEvent>(OnAfterInteract);
    }

    public void OnAfterInteract(EntityUid uid, DrugTestStickComponent component, AfterInteractEvent args)
    {
        if (!args.CanReach || args.Target == null || args.Handled)
            return;

        if (!component.IsUsed)
        {
            if (!HasComp<HungerComponent>(args.Target))
                return;

            if (TryComp<DnaComponent>(args.Target, out var dna))
            {
                component.DNA = dna.DNA;
            }
            else
            {
                component.DNA = Loc.GetString("drug-test-stick-dna");
            }


            if (TryComp<InstantDrugAddicationComponent>(args.Target, out var instantDrugAddication))
            {
                component.DependencyLevel = instantDrugAddication.DependencyLevel;
                component.AddictionLevel = instantDrugAddication.AddictionLevel;
                component.Tolerance = instantDrugAddication.Tolerance;
                component.WithdrawalLevel = instantDrugAddication.WithdrawalLevel;
                component.ThresholdTime = instantDrugAddication.SomeThresholdTime - instantDrugAddication.TimeLastAppointment;
            }

            _popup.PopupEntity(Loc.GetString("drug-test-stick-sample-taken"), args.User, args.User);
            component.IsUsed = true;
        }
        else
        {
            if (!HasComp<DrugInitializeMachineComponent>(args.Target))
                return;

            var ev = new StartDrugInitializeEvent(uid);
            RaiseLocalEvent(args.Target.Value, ref ev);
        }
    }
}
