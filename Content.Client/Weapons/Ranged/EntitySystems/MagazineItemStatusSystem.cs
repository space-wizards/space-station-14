using Content.Client.Magazine.Components;
using Content.Client.Magazine.UI;
using Content.Client.Items;

namespace Content.Client.Magazine.EntitySystems;

/// <summary>
/// Wires up item status logic for <see cref="MagazineItemStatusComponent"/>.
/// </summary>
/// <seealso cref="MagazineStatusControl"/>
public sealed class MagazineItemStatusSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        Subs.ItemStatus<MagazineItemStatusComponent>(
            entity => new MagazineStatusControl(entity, EntityManager));
    }
}
