using Robust.Shared.Utility;

namespace Content.Client.Stylesheets.SheetletConfigs;

public interface IWindowConfig : ISheetletConfig
{
    public ResPath WindowHeaderTexturePath { get; }
    public ResPath WindowHeaderAlertTexturePath { get; }
    public ResPath WindowBackgroundPath { get; }
    public ResPath WindowBackgroundBorderedPath { get; }
    public ResPath TransparentWindowBackgroundBorderedPath { get; }
}
