using Content.Shared.Bed.Components;
using Content.Shared.Buckle.Components;
using Content.Shared.Examine;

namespace Content.Shared.Bed.Systems
{
    public sealed class SharedStabilizeOnBuckleSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<StabilizeOnBuckleComponent, StrappedEvent>(OnStrapped);
            SubscribeLocalEvent<StabilizeOnBuckleComponent, UnstrappedEvent>(OnUnstrapped);
            SubscribeLocalEvent<StabilizeOnBuckleComponent, ExaminedEvent>(OnExamine);
        }

        private void OnStrapped(Entity<StabilizeOnBuckleComponent> bed, ref StrappedEvent args)
        {
            CopyComp(args.Strap.Owner, args.Buckle.Owner, bed.Comp);
        }

        private void OnUnstrapped(Entity<StabilizeOnBuckleComponent> bed, ref UnstrappedEvent args)
        {
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
