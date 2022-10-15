using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Roles;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Server.Jobs;

[UsedImplicitly]
public sealed class AddImplantSpecial : JobSpecial
{

    [DataField("implants", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<EntityPrototype>))]
    public HashSet<String> Implants { get; } = new();

    public override void AfterEquip(EntityUid mob)
    {
        var entMan = IoCManager.Resolve<IEntityManager>();
        var implanterSystem = entMan.System<SharedSubdermalImplantSystem>();
        var xformQuery = entMan.GetEntityQuery<TransformComponent>();

        if (!xformQuery.TryGetComponent(mob, out var xform))
            return;

        foreach (var implantId in Implants)
        {
            var implant = entMan.SpawnEntity(implantId, xform.Coordinates);
            var implantComp = entMan.GetComponent<SubdermalImplantComponent>(implant);

            implanterSystem.ForceImplant(mob, implant, implantComp);
        }
    }
}
