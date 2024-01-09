using Content.Shared.FixedPoint;
using Content.Shared.Research.Prototypes;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Implants.Components;

/// <summary>
/// ImplantLoaders can use materials to load implants into empty implanters.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ImplantLoaderComponent : Component
{
    [DataField("solutionName"), ViewVariables(VVAccess.ReadWrite)]
    public string SolutionName = "implantloader";

    /// <summary>
    /// Sound played when refilling the loader.
    /// </summary>
    [DataField]
    public SoundSpecifier Refill = new SoundPathSpecifier("/Audio/Effects/refill.ogg");

    /// <summary>
    /// Sound played when an implant is made.
    /// </summary>
    [DataField]
    public SoundSpecifier Success = new SoundPathSpecifier("/Audio/Effects/Cargo/ping.ogg");

    /// <summary>
    /// Sound played when an implant cannot be made.
    /// </summary>
    [DataField]
    public SoundSpecifier Failure = new SoundPathSpecifier("/Audio/Effects/Cargo/buzz_two.ogg");

    /// <summary>
    /// The recipes that this implant loader can make.
    /// </summary>
    [DataField("recipes"), ViewVariables]
    public List<ProtoId<ImplantRecipePrototype>> Recipes = new();
}

[NetSerializable, Serializable, Prototype("implantRecipe")]
public sealed partial class ImplantRecipePrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// What implant will this recipe make?
    /// </summary>
    [DataField]
    public string Product { get; set; } = default!;

    /// <summary>
    /// What reagent is needed to produce the implant?
    /// </summary>
    [DataField]
    public string Reagent { get; set; } = default!;

    /// <summary>
    /// How much does it cost to make one implant?
    /// </summary>
    [DataField("cost")]
    public FixedPoint2 CostPerUse = 50;
}
