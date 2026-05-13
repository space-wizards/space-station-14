using Content.Client.Weapons.Ranged.Components;
using Content.Client.Weapons.Ranged.UI;
using Content.Client.Items;

namespace Content.Client.Weapons.Ranged.EntitySystems;

/// <summary>
/// Wires up item status logic for <see cref="MagazineItemStatusComponent"/>.
/// </summary>
/// <seealso cref="MagazineStatusControl"/>
public sealed partial class MagazineItemStatusSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        Subs.ItemStatus<MagazineItemStatusComponent>(entity => new MagazineStatusControl(entity, EntityManager));
    }
}
