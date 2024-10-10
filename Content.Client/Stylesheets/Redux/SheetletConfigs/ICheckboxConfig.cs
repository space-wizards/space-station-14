using Robust.Shared.Utility;

namespace Content.Client.Stylesheets.Redux.SheetletConfigs;

public interface ICheckboxConfig
{
    public ResPath CheckboxUncheckedPath { get; }
    public ResPath CheckboxCheckedPath { get; }
}

