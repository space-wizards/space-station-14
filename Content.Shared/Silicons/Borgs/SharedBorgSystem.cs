using Content.Shared.Body.Systems;
using Content.Shared.Interaction;
using Content.Shared.Silicons.Borgs.Components;

namespace Content.Shared.Silicons.Borgs;

/// <summary>
/// This handles...
/// </summary>
public abstract class SharedBorgSystem : EntitySystem
{
    [Dependency] private readonly SharedBodySystem _body = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
    }
}
