using Content.Shared.Chemistry.Components;
using Content.Shared.Polymorph.Components;
using Content.Shared.Polymorph.Systems;
using Robust.Client.GameObjects;

namespace Content.Client.Polymorph.Systems;

public sealed class ChameleonProjectorSystem : SharedChameleonProjectorSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    private EntityQuery<AppearanceComponent> _appearanceQuery;

    public override void Initialize()
    {
        base.Initialize();

        _appearanceQuery = GetEntityQuery<AppearanceComponent>();

        SubscribeLocalEvent<ChameleonDisguiseComponent, AfterAutoHandleStateEvent>(OnHandleState);
    }

    private void OnHandleState(Entity<ChameleonDisguiseComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        CopyComp<SpriteComponent>(ent);
        CopyComp<GenericVisualizerComponent>(ent);
        CopyComp<SolutionContainerVisualsComponent>(ent);

        // reload appearance to hopefully prevent any invisible layers
        if (_appearanceQuery.TryComp(ent, out var appearance))
            _appearance.QueueUpdate(ent, appearance);
    }
}
