using Content.Client.Stylesheets.Redux;
using Robust.Client.UserInterface;

namespace Content.Client.Stylesheets;

public interface IStylesheetManager
{
    Stylesheet SheetNano { get; }
    Stylesheet SheetSpace { get; }
    Stylesheet SheetInterface { get; }

    void Initialize();

    public Stylesheet Init(BaseStylesheet baseSheet);
    public BaseStylesheet GetBaseStylesheet(Stylesheet? sheet);
}
