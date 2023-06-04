using Robust.Client.Graphics;

namespace Content.Client.Physics;

public sealed class JointVisualsSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();
        _overlay.AddOverlay(new JointVisualsOverlay(EntityManager));
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlay.RemoveOverlay<JointVisualsOverlay>();
    }
}
