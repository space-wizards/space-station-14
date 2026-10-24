using Robust.Shared.Prototypes;

namespace Content.Shared.Antag.Components;

/// <summary>
/// Ghost role spawner that creates an antag for the associated game rule.
/// </summary>
[RegisterComponent, Access(typeof(AntagSelectionSystem))]
public sealed partial class GhostRoleAntagSpawnerComponent : Component
{
    [DataField]
    public EntityUid? Rule;

    [DataField]
    public ProtoId<AntagSpecifierPrototype>? Definition;
}
