namespace Content.Shared.Parallax;

/// <summary>
/// Handles per-map parallax in sim. Out of sim parallax is handled by ParallaxManager.
/// </summary>
public abstract class SharedParallaxSystem : EntitySystem
{
    [Dependency] private readonly IViewVariablesManager _vvManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        _vvManager.GetTypeHandler<ParallaxComponent>()
            .AddPath(nameof(ParallaxComponent.Parallax), (_, comp) => comp.Parallax, (uid, value, comp) =>
            {
                if (!Resolve(uid, ref comp) ||
                    value.Equals(comp.Parallax))
                {
                    return;
                }

                comp.Parallax = value;
                Dirty(uid, comp);
            });
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _vvManager.GetTypeHandler<ParallaxComponent>().RemovePath(nameof(ParallaxComponent.Parallax));
    }
}
