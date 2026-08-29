using Content.Client.Fluids.UI;
using Content.Client.Items;
using Content.Shared.Fluids;
using Content.Shared.Item;
using Robust.Shared.Prototypes;

namespace Content.Client.Fluids;

/// <inheritdoc/>
public sealed partial class AbsorbentSystem : SharedAbsorbentSystem
{
    public ProtoId<ItemStatusPrototype> AbsorbentItemStatus = "Mop";

    public override void Initialize()
    {
        base.Initialize();
        Subs.ItemStatus<AbsorbentComponent>(ent => new AbsorbentItemStatus(ent, EntityManager), AbsorbentItemStatus);
    }
}
