using Content.Shared.Chemistry.Reagent;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Devour.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedDevourSystem))]
public sealed partial class DevourerComponent : Component
{
    [DataField]
    public ProtoId<EntityPrototype> DevourAction = "ActionDevour";

    [DataField]
    public EntityUid? DevourActionEntity;

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public SoundSpecifier? SoundDevour = new SoundPathSpecifier("/Audio/Effects/demon_consume.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f),
    };

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public float DevourTime = 3f;

    /// <summary>
    /// The amount of time it takes to devour something
    /// <remarks>
    /// NOTE: original intended design was to increase this proportionally with damage thresholds, but those proved quite difficult to get consistently. right now it devours the structure at a fixed timer.
    /// </remarks>
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public float StructureDevourTime = 10f;

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public SoundSpecifier? SoundStructureDevour = new SoundPathSpecifier("/Audio/Machines/airlock_creaking.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f),
    };

    /// <summary>
    /// Where the entities go when it devours them, empties when it is butchered.
    /// </summary>
    public Container Stomach = default!;

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public bool ShouldStoreDevoured = true;

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public EntityWhitelist? Whitelist = new()
    {
        Components = new[]
        {
            "MobState",
        }
    };

    /// <summary>
    /// The chemical ID injected into the dragon upon devouring
    /// </summary>
    [DataField]
    public ProtoId<ReagentPrototype> Chemical = "Ichor";

    /// <summary>
    /// The amount of ichor injected per devour
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public float HealSolutionSize = 15f;

    /// <summary>
    /// The chemical ID's injected into the victim upon being devoured.
    /// </summary>
    [DataField]
    public List<ProtoId<ReagentPrototype>> StomachChemicals = new();

    /// <summary>
    /// The amount of stomach chemicals injected into the devoured entity equally spread over each chemical.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public float StomachSolutionSize = 100f;

    /// <summary>
    /// The favorite food not only feeds you, but also heals
    /// </summary>
    [DataField]
    public FoodPreference FoodPreference = FoodPreference.All;
}
