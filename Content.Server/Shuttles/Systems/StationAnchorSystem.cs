using Content.Server.Shuttles.Components;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Server.Shuttles.Systems;

public sealed class StationAnchorSystem : EntitySystem
{
    [Dependency] private readonly ShuttleSystem _shuttleSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StationAnchorComponent, AnchorStateChangedEvent>(OnAnchorStationChange);
        SubscribeLocalEvent<StationAnchorComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
    }

    private void OnGetVerbs(EntityUid uid, StationAnchorComponent component, GetVerbsEvent<Verb> args)
    {
        // add debug verb to toggle power requirements
        args.Verbs.Add(new()
        {
            Text = "Enable",
            Category = VerbCategory.Debug,
            Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/VerbIcons/smite.svg.192dpi.png")), // "smite" is a lightning bolt
            Act = () => SetStatus(uid, true)
        });
    }

    private void OnAnchorStationChange(Entity<StationAnchorComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (!args.Anchored)
            SetStatus(ent, false);
    }

    private void SetStatus(EntityUid uid, bool enabled, ShuttleComponent? shuttleComponent = default)
    {
        var transform = Transform(uid);
        var grid = transform.GridUid;
        if (!grid.HasValue || !transform.Anchored && enabled || !Resolve(grid.Value, ref shuttleComponent))
            return;

        if (enabled)
        {
            _shuttleSystem.Disable(grid.Value);
        }
        else
        {
            _shuttleSystem.Enable(grid.Value);
        }

        shuttleComponent.Enabled = !enabled;
    }
}
