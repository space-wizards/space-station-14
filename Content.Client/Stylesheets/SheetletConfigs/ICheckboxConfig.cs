using Robust.Shared.Utility;

namespace Content.Client.Stylesheets.SheetletConfigs;

public interface ICheckboxConfig
{
    public ResPath CheckboxUncheckedPath { get; }
    public ResPath CheckboxCheckedPath { get; }
}

