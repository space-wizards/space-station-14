using Content.Shared.ParadoxClone;
using Robust.Shared.Prototypes;

namespace Content.Client.ParadoxClone;

public sealed partial class ParadoxCloneSystem : EntitySystem
{
    [Dependency] private IEntityManager _entMan = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;


    private static readonly EntProtoId WanderComponents = "ParadoxCloneWanderComponents";
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ParadoxCloneComponent, ActionParadoxCloneWanderEvent>(OnWander);
    }

    private void OnWander(Entity<ParadoxCloneComponent> ent, ref ActionParadoxCloneWanderEvent args)
    {
        // Makes the entity visible by adding components to it
        if (_proto.Resolve(WanderComponents, out var componentsEntity))
            _entMan.AddComponents(ent.Owner, componentsEntity.Components);
    }
}
