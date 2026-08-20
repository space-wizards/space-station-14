using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server.Jobs;

public sealed partial class AddComponentSpecial : JobSpecial
{
    [DataField(required: true)]
    public ComponentRegistry Components { get; private set; } = new();

    /// <summary>
    /// If this is true then existing components will be removed and replaced with these ones.
    /// </summary>
    [DataField]
    public bool RemoveExisting = true;

    public override void AfterEquip(EntityUid mob)
    {
        var entMan = IoCManager.Resolve<IEntityManager>();
        entMan.AddComponents(mob, Components, removeExisting: RemoveExisting);
    }

    public override void AfterUnequip(EntityUid mob)
    {
        var entMan = IoCManager.Resolve<IEntityManager>();
        foreach (var component in Components)
        {
            if (entMan.HasComponent(mob, component.GetType()))
                entMan.RemoveComponent(mob, component.GetType());
        }
    }
}
