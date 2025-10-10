using Content.Client.Items;
using Content.Shared.Crayon;

namespace Content.Client.Crayon;

public sealed class CrayonSystem : SharedCrayonSystem
{
    public override void Initialize()
    {
        base.Initialize();
        Subs.ItemStatus<CrayonComponent>(ent => new CrayonStatusControl(ent));
    }
}
