// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.Prototypes;
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
    public ProtoId<PaperworkFormPrototype>? ChosenForm { get; }
    public PhotocopierMode Mode { get; }
    public HashSet<PhotocopierFormCategory> AllowedFormCategories { get; }
    public bool WasEmagged { get; }
    public int TonerLeft { get; }
    public int MaxTonerAmount { get; }

    public PhotocopierUiState(
        bool canPrint,
        bool isPaperInserted,
        ProtoId<PaperworkFormPrototype>? chosenForm,
        PhotocopierMode mode,
        HashSet<PhotocopierFormCategory> allowedFormCategories,
        bool wasEmagged,
        int tonerLeft,
        int maxTonerAmount)
    {
        CanPrint = canPrint;
        IsPaperInserted = isPaperInserted;
        ChosenForm = chosenForm;
        Mode = mode;
        AllowedFormCategories = allowedFormCategories;
        WasEmagged = wasEmagged;
        TonerLeft = tonerLeft;
        MaxTonerAmount = maxTonerAmount;
    }
}

[Serializable, NetSerializable]
public sealed class PhotocopierChoseFormMessage : BoundUserInterfaceMessage
{
    public readonly ProtoId<PaperworkFormPrototype> PaperworkForm;

    public PhotocopierChoseFormMessage(ProtoId<PaperworkFormPrototype> paperworkForm)
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

public enum PhotocopierFormCategory // Ideally, it should be its own FormCategoryPrototype. But for now it will be like this.
{
    NTCargo,
    NTCivilian,
    NTEngineering,
    NTLaw,
    NTMedical,
    NTScience,
    NTSecurity,
    NTCommand,
    NTCentcomm,
    NTOperator,
    Syndicate,
    Nukeops,
}
