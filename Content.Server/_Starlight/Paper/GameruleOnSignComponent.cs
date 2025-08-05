using Content.Shared.GameTicking.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Component = Robust.Shared.GameObjects.Component;

namespace Content.Server._Starlight.Paper;

[RegisterComponent]
public sealed partial class GameruleOnSignComponent : Component
{
    /// <summary>
    /// how many signatures are needed before this paper goes into effect.
    /// </summary>
    [DataField]
    public int Remaining = 1;

    /// <summary>
    /// A list of every entity that has signed this paper to prevent spam signing from instantly activating the paper.
    /// </summary>
    [ViewVariables]
    public List<EntityUid> SignedEntityUids = [];

    /// <summary>
    /// A Whitelist of whos signatures should count for this component.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist = null;

    /// <summary>
    /// A Whitelist of whos signatures should not count for this component.
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist = null;

    /// <summary>
    /// What is the chance of this going on after all the signatures are collected. 1 is always, 0 is never.
    /// </summary>
    [DataField]
    public float Chance = 1.0f;

    /// <summary>
    /// What game rules are added once signatures are collected and with a bit of luck.
    /// </summary>
    [DataField]
    public List<EntProtoId<GameRuleComponent>> Rules = [];
    
    /// <summary>
    /// is the faxable component kept? this is for admeme protos
    /// </summary>
    [DataField]
    public bool KeepFaxable = false;
}
