using Robust.Shared.Prototypes;
using Component = Robust.Shared.GameObjects.Component;

namespace Content.Server._Starlight.Paper;

[RegisterComponent]
public sealed partial class AntagOnSignComponent : Component
{
    /// <summary>
    /// how many people are able to sign this paper in a attempt to roll for antag.
    /// </summary>
    [DataField("charges")]
    public int ChargesRemaing = 1;

    /// <summary>
    /// A list of every entity that has signed this paper to prevent spam signing from using all the charges
    /// </summary>
    [ViewVariables]
    public List<EntityUid> SignedEntityUids = [];

    /// <summary>
    /// What is the chance of this signature procing and making them a antag with 1 being always and 0 being never
    /// </summary>
    [DataField]
    public float Chance = 1.0f;

    /// <summary>
    /// should we spawn a paradox clone of the person signing this. technically not making them a antag but it works nearly the same
    /// </summary>
    [DataField("spawnParadoxClone")]
    public bool ParadoxClone = false;

    /// <summary>
    /// what antags should be added to the person.
    /// </summary>
    [DataField]
    public List<AntagCompPair> Antags = [];
}

[DataDefinition]
public sealed partial class AntagCompPair
{
    [DataField]
    public EntProtoId Antag;

    /// <summary>
    /// Icky evil raw string but there is no `Component` Seriliazable type
    /// </summary>
    [DataField]
    public string TargetComponent;
}
