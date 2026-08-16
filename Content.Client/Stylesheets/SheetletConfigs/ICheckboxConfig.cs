using Robust.Shared.Utility;

namespace Content.Client.Stylesheets.SheetletConfigs;

public interface ICheckboxConfig : ISheetletConfig
{
    public ResPath CheckboxUncheckedPath { get; }
    public ResPath CheckboxCheckedPath { get; }
}

