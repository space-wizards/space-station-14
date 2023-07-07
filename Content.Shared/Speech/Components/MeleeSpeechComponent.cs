using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Speech.EntitySystems;
using Robust.Shared.Serialization;

namespace Content.Shared.Speech.Components;

[RegisterComponent]
[AutoGenerateComponentState]
public sealed partial class MeleeSpeechComponent : Component
{

	[ViewVariables(VVAccess.ReadWrite)]
	[DataField("Battlecry")]
	[AutoNetworkedField]
	public string? Battlecry;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("MaxBattlecryLength")]
    public int MaxBattlecryLength = 12;
    /*
    [DataField("configureAction", customTypeSerializer: typeof(PrototypeIdSerializer<InstantActionPrototype>))]
    public string configureActionId = "Set Battlecry";
    */
    [DataField("configureAction")] 
    public InstantAction? ConfigureAction = null;
}

/// <summary>
/// Key representing which <see cref="BoundUserInterface"/> is currently open.
/// Useful when there are multiple UI for an object. Here it's future-proofing only.
/// </summary>/
[Serializable, NetSerializable]
public enum MeleeSpeechUiKey : byte
{
    Key,
}

/*[Serializable, NetSerializable]
public sealed class MeleeSpeechConfigureActionEvent : InstantActionEvent
{
    public EntityUid User { get; }
    public EntityUid Target { get; }

    public MeleeSpeechConfigureActionEvent(EntityUid who, EntityUid target)
    {
        User = who;
        Target = target;
    }
}*/

/// <summary>
/// Represents an <see cref="MeleeSpeechComponent"/> state that can be sent to the client
/// </summary>
[Serializable, NetSerializable]
public sealed class MeleeSpeechBoundUserInterfaceState : BoundUserInterfaceState
{
    public string CurrentBattlecry { get; }

    public MeleeSpeechBoundUserInterfaceState(string currentBattlecry)
    {
        CurrentBattlecry = currentBattlecry;
    }
}

[Serializable, NetSerializable]
public sealed class MeleeSpeechBattlecryChangedMessage : BoundUserInterfaceMessage
{
    public string Battlecry { get; }
    public MeleeSpeechBattlecryChangedMessage(string battlecry)
    {
        Battlecry = battlecry;
    }
}

[Serializable, NetSerializable]
public sealed class MeleeSpeechConfigureActionMessage : BoundUserInterfaceMessage
{
    public string CurrentBattlecry { get; }

    public MeleeSpeechConfigureActionMessage(string currentBattlecry)
    {
        CurrentBattlecry = currentBattlecry;
    }
}
