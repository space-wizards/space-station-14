using Content.Shared.Actions;
using Content.Shared.Bed.Components;
using Content.Shared.Bed.Sleep;
using Content.Shared.Buckle.Components;
using Content.Shared.Examine;
using Robust.Shared.Utility;

namespace Content.Shared.Bed.Systems
{
    public sealed class SharedStabilizeOnBuckleSystem : EntitySystem
    {
        [Dependency] private readonly SleepingSystem _sleepingSystem = null!;
        [Dependency] private readonly SharedActionsSystem _actionsSystem = null!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<StabilizeOnBuckleComponent, StrappedEvent>(OnStrapped);
            SubscribeLocalEvent<StabilizeOnBuckleComponent, UnstrappedEvent>(OnUnstrapped);
            SubscribeLocalEvent<StabilizeOnBuckleComponent, ExaminedEvent>(OnExamine);
        }

        private void OnStrapped(Entity<StabilizeOnBuckleComponent> bed, ref StrappedEvent args)
        {
            _actionsSystem.AddAction(args.Buckle, ref bed.Comp.SleepAction, SleepingSystem.SleepActionId, bed);
            CopyComp(args.Strap.Owner, args.Buckle.Owner, bed.Comp);
            // Single action entity, cannot strap multiple entities to the same rollerbed.
            DebugTools.AssertEqual(args.Strap.Comp.BuckledEntities.Count, 1);
        }

        private void OnUnstrapped(Entity<StabilizeOnBuckleComponent> bed, ref UnstrappedEvent args)
        {
            _actionsSystem.RemoveAction(args.Buckle.Owner, bed.Comp.SleepAction);
            _sleepingSystem.TryWaking(args.Buckle.Owner);
            RemComp<StabilizeOnBuckleComponent>(args.Buckle.Owner);
        }

        private void OnExamine(Entity<StabilizeOnBuckleComponent> ent, ref ExaminedEvent args)
        {
            if (!HasComp<StrapComponent>(ent))
            {
                return;
            }
            using (args.PushGroup(nameof(StabilizeOnBuckleComponent)))
            {
                var comp = ent.Comp;
                var value = MathF.Round((comp.Efficiency) * 100, 1);
                args.PushMarkup(Loc.GetString("stabilizing-efficiency-value", ("value", value)));
                if (comp.ReducesBleeding > 0)
                    args.PushMarkup(Loc.GetString("stabilizing-bleeding"));
            }
        }
    }
}
