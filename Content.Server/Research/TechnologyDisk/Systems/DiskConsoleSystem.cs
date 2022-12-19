using Content.Server.Research.TechnologyDisk.Components;
using Content.Server.UserInterface;
using Robust.Server.GameObjects;

namespace Content.Server.Research.TechnologyDisk.Systems;

/// <summary>
/// This handles...
/// </summary>
public sealed class DiskConsoleSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<DiskConsoleComponent, BeforeActivatableUIOpenEvent>(OnBeforeUiOpen);
    }

    private void OnBeforeUiOpen(EntityUid uid, DiskConsoleComponent component, BeforeActivatableUIOpenEvent args)
    {
        UpdateUserInterface(uid, component);
    }

    public void UpdateUserInterface(EntityUid uid, DiskConsoleComponent component)
    {

    }
}
