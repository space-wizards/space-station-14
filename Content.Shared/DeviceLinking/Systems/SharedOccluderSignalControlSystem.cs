using Content.Shared.DeviceLinking.Components;

namespace Content.Shared.DeviceLinking.Systems;

/// <summary>
/// Controls an <see cref="OccluderComponent"/> through device signals.
/// <seealso cref="OccluderSignalControlComponent"/>
/// </summary>
public abstract class SharedOccluderSignalControlSystem : EntitySystem
{
    [Dependency] private readonly SharedDeviceLinkSystem _deviceLink = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OccluderSignalControlComponent, ComponentInit>(OnInit);
    }

    private void OnInit(Entity<OccluderSignalControlComponent> ent, ref ComponentInit args)
    {
        _deviceLink.EnsureSinkPorts(ent.Owner, ent.Comp.EnablePort, ent.Comp.DisablePort, ent.Comp.TogglePort);
    }
}
