using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Roles;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server.Jobs;

/// <summary>
/// Adds implants on spawn to the entity
/// </summary>
[UsedImplicitly]
public sealed partial class AddImplantSpecial : JobSpecial
{
    [DataField]
    public HashSet<EntProtoId> Implants { get; private set; } = new();

    public override void AfterEquip(EntityUid mob)
    {
        var entMan = IoCManager.Resolve<IEntityManager>();
        var implantSystem = entMan.System<SharedSubdermalImplantSystem>();
        implantSystem.AddImplants(mob, Implants);
    }

    public override void AfterUnequip(EntityUid mob)
    {
        var entMan = IoCManager.Resolve<IEntityManager>();
        var implantSystem = entMan.System<SharedSubdermalImplantSystem>();
        if (!entMan.TryGetComponent<ImplantedComponent>(mob, out var implantComp))
            return;

        var implantsToRemove = new HashSet<EntityUid>();

        foreach (var implant in implantComp.ImplantContainer.ContainedEntities)
        {
            var entProto = entMan.GetComponent<MetaDataComponent>(implant).EntityPrototype;
            if (entProto != null)
            {
                if (Implants.Contains(entProto.ID))
                    implantsToRemove.Add(implant);
            }
        }

        foreach (var implant in implantsToRemove)
        {
            implantSystem.ForceRemove(mob, implant);
        }
    }
}
