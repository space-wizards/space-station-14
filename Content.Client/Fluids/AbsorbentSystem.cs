using Content.Client.Fluids.UI;
using Content.Client.Items;
using Content.Shared.Fluids;
using Robust.Client.UserInterface;

namespace Content.Client.Fluids;

/// <inheritdoc/>
public sealed class AbsorbentSystem : SharedAbsorbentSystem
{
    public override void Initialize()
    {
        base.Initialize();
        Subs.ItemStatus<AbsorbentComponent>(GetAbsorbent);
    }

    private Control GetAbsorbent(EntityUid arg)
    {
        return new AbsorbentItemStatus(arg, EntityManager);
    }
}
