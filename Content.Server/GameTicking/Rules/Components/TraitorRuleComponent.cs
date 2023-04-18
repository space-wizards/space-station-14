using Content.Server.Traitor;
using Content.Shared.CCVar;
using Content.Shared.Preferences;
using Robust.Server.Player;
using Robust.Shared.Audio;

namespace Content.Server.GameTicking.Rules.Components;

[RegisterComponent]
public sealed class TraitorRuleComponent : Component
{
    public readonly SoundSpecifier _addedSound = new SoundPathSpecifier("/Audio/Misc/tatoralert.ogg");
    public List<TraitorRole> Traitors = new();

    public string TraitorPrototypeID = "Traitor";
    public string TraitorUplinkPresetId = "StorePresetUplink";

    public int TotalTraitors => Traitors.Count;
    public string[] Codewords = new string[3];


    public enum SelectionState
    {
        WaitingForSpawn = 0,
        ReadyToSelect = 1,
        SelectionMade = 2,
    }

    public SelectionState SelectionStatus = SelectionState.WaitingForSpawn;
    public TimeSpan _announceAt = TimeSpan.Zero;
    public Dictionary<IPlayerSession, HumanoidCharacterProfile> _startCandidates = new();
}
