// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.Serialization;

namespace Content.Shared.DeadSpace.Photocopier;

[Serializable, NetSerializable]
public enum PhotocopierUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class PhotocopierUiState : BoundUserInterfaceState
{
    public bool CanPrint { get; }
    public bool IsPaperInserted { get; }
    public PaperworkFormPrototype? ChosenForm { get; }
    public PhotocopierMode Mode { get; }
    public PhotocopierType Type { get; }
    public bool WasEmagged { get; }
    public int TonerLeft { get; }
    public int MaxTonerAmount { get; }

    public PhotocopierUiState(
        bool canPrint,
        bool isPaperInserted,
        PaperworkFormPrototype? chosenForm,
        PhotocopierMode mode,
        PhotocopierType type,
        bool wasEmagged,
        int tonerLeft,
        int maxTonerAmount)
    {
        CanPrint = canPrint;
        IsPaperInserted = isPaperInserted;
        ChosenForm = chosenForm;
        Mode = mode;
        Type = type;
        WasEmagged = wasEmagged;
        TonerLeft = tonerLeft;
        MaxTonerAmount = maxTonerAmount;
    }
}

[Serializable, NetSerializable]
public sealed class PhotocopierChoseFormMessage : BoundUserInterfaceMessage
{
    public readonly PaperworkFormPrototype PaperworkForm;

    public PhotocopierChoseFormMessage(PaperworkFormPrototype paperworkForm)
    {
        PaperworkForm = paperworkForm;
    }
}

[Serializable, NetSerializable]
public sealed class PhotocopierPrintMessage : BoundUserInterfaceMessage
{
    public readonly int Amount;
    public readonly PhotocopierMode Mode;

    public PhotocopierPrintMessage(int amount, PhotocopierMode mode)
    {
        Amount = amount;
        Mode = mode;
    }
}

[Serializable, NetSerializable]
public sealed class PhotocopierCopyModeMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class PhotocopierPrintModeMessage : BoundUserInterfaceMessage
{
}

public enum PhotocopierMode
{
    Copy,
    Print,
}

public enum PhotocopierType
{
    Default,
    Command,
    Centcomm,
    Syndicate,
    Nukeops,
}
