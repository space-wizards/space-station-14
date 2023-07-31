using Content.Shared.Audio;
using Robust.Client.Graphics;
using Robust.Shared.Enums;

namespace Content.Client.Audio;

/// <summary>
/// Debug overlay that shows all ambientsound sources in range
/// </summary>
public sealed class AmbientSoundOverlay : Overlay
{
    private IEntityManager _entManager;
    private AmbientSoundSystem _ambient;
    private EntityLookupSystem _lookup;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public AmbientSoundOverlay(IEntityManager entManager, AmbientSoundSystem ambient, EntityLookupSystem lookup)
    {
        _entManager = entManager;
        _ambient = ambient;
        _lookup = lookup;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var worldHandle = args.WorldHandle;
        var ambientQuery = _entManager.GetEntityQuery<AmbientSoundComponent>();
        var xformQuery = _entManager.GetEntityQuery<TransformComponent>();

        const float Size = 0.25f;
        const float Alpha = 0.25f;

        foreach (var ent in _lookup.GetEntitiesIntersecting(args.MapId, args.WorldBounds))
        {
            if (!ambientQuery.TryGetComponent(ent, out var ambientSound) ||
                !xformQuery.TryGetComponent(ent, out var xform)) continue;

            if (ambientSound.Enabled)
            {
                if (_ambient.IsActive(ambientSound))
                {
                    worldHandle.DrawCircle(xform.WorldPosition, Size, Color.LightGreen.WithAlpha(Alpha * 2f));
                }
                else
                {
                    worldHandle.DrawCircle(xform.WorldPosition, Size, Color.Orange.WithAlpha(Alpha));
                }
            }
            else
            {
                worldHandle.DrawCircle(xform.WorldPosition, Size, Color.Red.WithAlpha(Alpha));
            }
        }
    }
}
