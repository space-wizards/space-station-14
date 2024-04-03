using Content.Shared.Polymorph.Components;
using Content.Shared.Polymorph.Systems;
using Robust.Client.GameObjects;

namespace Content.Client.Polymorph.Systems;

public sealed class ChameleonProjectorSystem : SharedChameleonProjectorSystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<ChameleonDisguiseComponent, AfterAutoHandleStateEvent>(OnHandleState);
    }

    private void OnHandleState(Entity<ChameleonDisguiseComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        CopyComp<SpriteComponent>(ent);
        // support well written things' appearances but not older code that has their own visualizer components
        if (CopyComp<AppearanceComponent>(ent))
            CopyComp<GenericVisualizerComponent>(ent);
    }
}
