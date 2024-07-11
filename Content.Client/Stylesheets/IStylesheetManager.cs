using Content.Client.Stylesheets.Redux;
using Robust.Client.UserInterface;

namespace Content.Client.Stylesheets;

public interface IStylesheetManager
{
    Stylesheet SheetNano { get; }
    Stylesheet SheetSpace { get; }
    Stylesheet SheetSystem { get; }

    void Initialize();
}
