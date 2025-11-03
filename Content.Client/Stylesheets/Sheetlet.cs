using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;

namespace Content.Client.Stylesheets;

public abstract class Sheetlet<T>
{
    [Dependency] protected readonly IResourceCache ResCache = default!;

    protected Sheetlet()
    {
        IoCManager.InjectDependencies(this);
    }

    public abstract StyleRule[] GetRules(T sheet, object config);
}
