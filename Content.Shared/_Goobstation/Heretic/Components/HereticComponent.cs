using Content.Shared.Heretic.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Heretic;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HereticComponent : Component
{
    #region Prototypes

    [DataField] public List<ProtoId<HereticKnowledgePrototype>> BaseKnowledge = new()
    {
        "BreakOfDawn",
        "HeartbeatOfMansus",
        "AmberFocus",
        "LivingHeart",
        "CodexCicatrix",
    };

    #endregion

    [DataField, AutoNetworkedField] public List<ProtoId<HereticRitualPrototype>> KnownRituals = new();
    [DataField] public ProtoId<HereticRitualPrototype>? ChosenRitual;

    /// <summary>
    ///     Contains the list of targets that are eligible for sacrifice.
    /// </summary>
    [DataField, AutoNetworkedField] public List<NetEntity?> SacrificeTargets = new();

    /// <summary>
    ///     How much targets can a heretic have?
    /// </summary>
    [DataField, AutoNetworkedField] public int MaxTargets = 5;

    // hardcoded paths because i hate it
    // "Ash", "Lock", "Flesh", "Void", "Blade", "Rust"
    /// <summary>
    ///     Indicates a path the heretic is on.
    /// </summary>
    [DataField, AutoNetworkedField] public string? CurrentPath = null;

    /// <summary>
    ///     Indicates a stage of a path the heretic is on. 0 is no path, 10 is ascension
    /// </summary>
    [DataField, AutoNetworkedField] public int PathStage = 0;

    [DataField, AutoNetworkedField] public bool Ascended = false;

    /// <summary>
    ///     Used to prevent double casting mansus grasp.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)] public bool MansusGraspActive = false;

    /// <summary>
    ///     Indicates if a heretic is able to cast advanced spells.
    ///     Requires wearing focus, codex cicatrix, hood or anything else that allows him to do so.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)] public bool CanCastSpells = false;
}
