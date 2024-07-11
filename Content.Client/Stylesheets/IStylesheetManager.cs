using Content.Client.Stylesheets.Redux;
using Robust.Client.UserInterface;

namespace Content.Client.Stylesheets;

public interface IStylesheetManager
{
    Stylesheet SheetNanotransen { get; }
    Stylesheet SheetSystem { get; }

    public Dictionary<string, Stylesheet> Stylesheets { get; }

    void Initialize();
}
