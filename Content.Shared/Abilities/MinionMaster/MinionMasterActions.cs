using Content.Shared.Actions;

namespace Content.Shared.Abilities.MinionMaster;

public sealed partial class MinionMasterRaiseArmyActionEvent : InstantActionEvent
{

}

public sealed partial class MinionMasterOrderActionEvent : InstantActionEvent
{
    /// <summary>
    /// The type of order being given
    /// </summary>
    [DataField("type")]
    public MinionOrderType Type;
}
