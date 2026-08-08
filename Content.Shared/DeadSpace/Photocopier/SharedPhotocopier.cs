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
    // DS14-start
    // Deliberately not added to any photocopier's AllowedFormCategories - the discipline order
    // form is only reachable through the Personnel Records console's print button, which fills in
    // placeholders (author/target/sanction/reason) that PhotocopierSystem.PrintForm doesn't know
    // about. Printing it from a regular photocopier would produce a paper with those tokens still
    // literally in the text.
    NTPersonnel,
    // DS14-end
}

// DS14-start
/// <summary>
/// The four placeholder substitutions every printed paperwork form gets, regardless of who prints
/// it: document title, time-in-shift, in-universe date, station name. Extracted out of
/// <c>PhotocopierSystem.PrintForm</c> so <c>PersonnelPrintingSystem</c> (which needs the same four
/// plus its own set) can't drift out of sync with it after a future date-format change.
/// </summary>
public static class PaperworkTextSubstitutions
{
    public static string ApplyBase(string text, string documentName, TimeSpan roundDuration, string? stationName)
    {
        text = text.Replace("DOCUMENT NAME", documentName);
        text = text.Replace("{{HOUR.MINUTE.SECOND}}", roundDuration.ToString("hh\\:mm\\:ss"));
        text = text.Replace("{{DAY.MONTH.YEAR}}", DateTime.UtcNow.AddHours(3).ToString("dd.MM") + ".2710");

        if (stationName != null)
            text = text.Replace("STATION XX-00", stationName);

        return text;
    }
}
// DS14-end
