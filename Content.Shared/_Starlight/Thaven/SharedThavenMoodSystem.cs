using Content.Shared.Emag.Systems;
using Content.Shared._Starlight.Thaven.Components;
using Robust.Shared.Prototypes;
using Content.Shared.Dataset;

namespace Content.Shared._Starlight.Thaven;

public abstract class SharedThavenMoodSystem : EntitySystem
{
    
    public static readonly ProtoId<DatasetPrototype> YesAndDataset = "ThavenMoodsYesAnd";  
    public static readonly ProtoId<DatasetPrototype> NoAndDataset = "ThavenMoodsNoAnd";
    public static readonly ProtoId<DatasetPrototype> WildcardDataset = "ThavenMoodsWildcard";

    [Dependency] private readonly EmagSystem _emag = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ThavenMoodsComponent, GotEmaggedEvent>(OnEmagged);
    }

    protected virtual void OnEmagged(Entity<ThavenMoodsComponent> ent, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
            return;

        // allow repeated-emagging of thaven
        // if (_emag.CheckFlag(ent, EmagType.Interaction))
        //     return;

        // allow self-emagging of thaven
        // if (ent.Owner == args.UserUid)
        //     return;

        args.Handled = true;
    }
}
