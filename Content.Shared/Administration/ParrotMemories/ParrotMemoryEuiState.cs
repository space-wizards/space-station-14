using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration.ParrotMemories;

/// <summary>
/// Class used to represent the state of a parrotUi
/// </summary>
[Serializable, NetSerializable]
public sealed class ParrotMemoryEuiState(List<ExtendedParrotMemory> memories, int currentRoundId, int selectedRoundId) : EuiStateBase
{
    public List<ExtendedParrotMemory> Memories { get; } = memories;
    public int CurrentRoundId = currentRoundId;
    public int SelectedRoundId = selectedRoundId;
}

/// <summary>
/// Used to request a refresh of the parrot memory ui state
/// </summary>
/// <param name="showBlocked">Whether to return memories that are blocked or not</param>
/// <param name="textFilter">String by which to filter. No filtering is done if this is empty</param>
/// <param name="requestedRoundId">Round ID that is requested. If this is null, the current round is requested</param>
[Serializable, NetSerializable]
public sealed class ParrotMemoryRefreshMsg(bool showBlocked, string textFilter, int? requestedRoundId) : EuiMessageBase
{
    public bool ShowBlocked { get; } = showBlocked;
    public string TextFilter { get; } = textFilter;
    public int? RequestedRoundId { get; } = requestedRoundId;
}

/// <summary>
/// Class used to send a message requesting a parrot memory be blocked
/// </summary>
[Serializable, NetSerializable]
public sealed class SetParrotMemoryBlockedMsg(int memoryId, bool block) : EuiMessageBase
{
    public int MemoryId { get; } = memoryId;
    public bool Block { get; } = block;
}
