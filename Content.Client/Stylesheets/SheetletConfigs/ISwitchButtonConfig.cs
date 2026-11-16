using Robust.Shared.Utility;

namespace Content.Client.Stylesheets.SheetletConfigs;

public interface ISwitchButtonConfig
{
    public ResPath SwitchButtonTrackFillPath { get; }
    public ResPath SwitchButtonTrackOutlinePath { get; }
    public ResPath SwitchButtonThumbFillPath { get; }
    public ResPath SwitchButtonThumbOutlinePath { get; }
    public ResPath SwitchButtonSymbolOffPath { get; }
    public ResPath SwitchButtonSymbolOnPath { get; }
}

