using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Component = Robust.Shared.GameObjects.Component;

namespace Content.Server._Starlight.Paper;

[RegisterComponent]
public sealed partial class ObjectiveOnSignComponent : Component
{
    /// <summary>
    /// how many people are able to sign this paper in a attempt to roll for antag.
    /// </summary>
    [DataField("charges")]
    public int ChargesRemaining = 1;

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
    /// what objectives should be added to the person.
    /// </summary>
    [DataField]
    public List<EntProtoId> Objectives = [];

    /// <summary>
    /// true to add objectives. false to delete all objectives and add only these.
    /// </summary>
    [DataField]
    public bool Append = false;

    /// <summary>
    /// whitelist the entity must pass to be allowed to get objectives
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist = null;

    /// <summary>
    /// blacklist the entity must fail to be allowed to get objectives
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist = null;


    /// <summary>
    /// is the faxable component kept? this is for admeme protos
    /// </summary>
    [DataField]
    public bool KeepFaxable = false;
}
