using Robust.Client.UserInterface;

namespace Content.Client.Stylesheets;

public interface IStylesheetManager
{
    Stylesheet SheetNanotransen { get; }
    Stylesheet SheetSystem { get; }

    [Obsolete("Update to use SheetNanotransen instead")]
    Stylesheet SheetNano { get; }
    [Obsolete("Update to use SheetSystem instead")]
    Stylesheet SheetSpace { get; }

    public Dictionary<string, Stylesheet> Stylesheets { get; }

    void Initialize();
}
