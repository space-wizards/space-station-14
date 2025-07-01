namespace Content.Server.Traits.Assorted;

public sealed partial class HemophiliaSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<HemophiliaComponent, BleedStackReduceEvent>(OnBleedStackReduceEvent);
    }

    private void OnBleedStackReduceEvent(Entity<HemophiliaComponent> ent, ref BleedStackReduceEvent args)
    {
        args.BleedStackReductionAmount = ent.Comp.HemophiliacBleedReductionAmount;
    }
}

