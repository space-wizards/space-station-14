using Content.Client.Fluids.UI;
using Content.Client.Items;
using Content.Shared.Fluids;

namespace Content.Client.Fluids;

/// <inheritdoc/>
public sealed class AbsorbentSystem : SharedAbsorbentSystem
{
    public override void Initialize()
    {
        base.Initialize();
        Subs.ItemStatus<AbsorbentComponent>(ent => new AbsorbentItemStatus(ent, EntityManager));
    }
}
