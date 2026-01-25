using Content.Server.Anomaly.Effects;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using System.Numerics;

namespace Content.Server.Anomaly.Components;
/// <summary>
/// This component allows the anomaly to generate a random type of reagent in the specified SolutionContainer.
/// With the increasing severity of the anomaly, the type of reagent produced may change.
/// The higher the severity of the anomaly, the higher the chance of dangerous or useful reagents.
/// </summary>
[RegisterComponent, Access(typeof(ReagentProducerAnomalySystem))]
public sealed partial class ReagentProducerAnomalyComponent : Component
{
    //the addition of the reagent will occur instantly when an anomaly appears,
    //and there will not be the first three seconds of a white empty anomaly.
    public float AccumulatedFrametime = 3.0f;
    /// <summary>
    ///     How frequently should this reagent generation update, in seconds?
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float UpdateInterval = 3.0f;

    /// <summary>
    /// The spread of the random weight of the choice of this category, depending on the severity.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public Vector2 WeightSpreadDangerous = new(5.0f, 9.0f);
    /// <summary>
    /// The spread of the random weight of the choice of this category, depending on the severity.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public Vector2 WeightSpreadFun = new(3.0f, 0.0f);
    /// <summary>
    /// The spread of the random weight of the choice of this category, depending on the severity.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public Vector2 WeightSpreadUseful = new(1.0f, 1.0f);

    /// <summary>
    /// Category of dangerous reagents for injection. Various toxins and poisons
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public List<ProtoId<ReagentPrototype>> DangerousChemicals = new();
    /// <summary>
    /// Category of useful reagents for injection. Medicine and other things that players WANT to get
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public List<ProtoId<ReagentPrototype>> UsefulChemicals = new();
    /// <summary>
    /// Category of fun reagents for injection. Glue, drugs, beer. Something that will bring fun.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public List<ProtoId<ReagentPrototype>> FunChemicals = new();

    /// <summary>
    /// Noise made when anomaly pulse.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier ChangeSound = new SoundPathSpecifier("/Audio/Effects/waterswirl.ogg");
    /// <summary>
    /// The component will repaint the sprites of the object to match the current color of the solution,
    /// if the RandomSprite component is hung correctly.
    /// Ideally, this should be put into a separate component, but I suffered for 4 hours,
    /// and nothing worked out for me. So for now it will be like this.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public bool NeedRecolor = false;

    /// <summary>
    /// the maximum amount of reagent produced per second
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float MaxReagentProducing = 1.5f;

    /// <summary>
    /// how much does the reagent production increase before entering the supercritical state
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float SupercriticalReagentProducingModifier = 100f;

    /// <summary>
    /// The name of the reagent that the anomaly produces.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<ReagentPrototype> ProducingReagent = "Water";

    /// <summary>
    /// Solution name where the substance is generated
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("solution")]
    public string SolutionName = "default";

    /// <summary>
    /// Solution where the substance is generated
    /// </summary>
    [ViewVariables]
    public Entity<SolutionComponent>? Solution = null;
}
