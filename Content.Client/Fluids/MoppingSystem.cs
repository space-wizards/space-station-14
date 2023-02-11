using Content.Client.Fluids.UI;
using Content.Client.Items;
using Content.Shared.Fluids;
using Robust.Client.UserInterface;

namespace Content.Client.Fluids;

public sealed class MoppingSystem : SharedMoppingSystem
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
