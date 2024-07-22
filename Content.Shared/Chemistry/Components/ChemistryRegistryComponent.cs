using Content.Shared.Chemistry.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Chemistry.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedChemistryRegistrySystem))]
public sealed partial class ChemistryRegistryComponent : Component
{
    [DataField, AutoNetworkedField]
    public string ContainerId = "chemistry_registry_container";

    [DataField, AutoNetworkedField]
    public Dictionary<string, EntityUid> Reagents = new();

    [DataField, AutoNetworkedField]
    public Dictionary<string, EntityUid> Reactions = new();
}
