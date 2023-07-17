using Robust.Shared.Serialization;

namespace Content.Shared._FTL.Economy;

[Serializable, NetSerializable]
public sealed class IdAtmUiState : BoundUserInterfaceState
{
    public string IdName { get; }
    public bool IdCardIn { get; }
    public bool IdCardLocked { get; }
    public int Bank { get; }
    public int Cash { get; }

    public IdAtmUiState(string name, int bank, int cash, bool idCardIn, bool idCardLocked)
    {
        IdName = name;
        Bank = bank;
        Cash = cash;
        IdCardIn = idCardIn;
        IdCardLocked = idCardLocked;
    }
}
