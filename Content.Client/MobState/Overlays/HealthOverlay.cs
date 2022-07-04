using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client.MobState.Overlays;

public sealed class HealthOverlay : Overlay
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEntityManager _entities = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    private readonly ShaderInstance _critShader;
    private readonly ShaderInstance _deadShader;

    public int Level { get; set; }

    public HealthOverlay()
    {
        // TODO: Replace
        IoCManager.InjectDependencies(this);
        _critShader = _prototypeManager.Index<ShaderPrototype>("GradientCircleMask").Instance();
        _deadShader = _prototypeManager.Index<ShaderPrototype>("CircleMask").Instance();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var viewport = args.WorldAABB;
        var worldHandle = args.WorldHandle;

        switch (Level)
        {
            case 1:
                break;
            case MobStateSystem.Levels:
                worldHandle.UseShader(_deadShader);
                worldHandle.DrawRect(viewport, Color.White);
                break;
            case MobStateSystem.Levels - 1:
                worldHandle.UseShader(_critShader);
                worldHandle.DrawRect(viewport, Color.White);
                break;
        }
    }
}
