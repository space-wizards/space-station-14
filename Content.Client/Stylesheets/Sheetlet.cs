using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;

namespace Content.Client.Stylesheets;

public abstract partial class Sheetlet<T>
{
    [Dependency] protected IResourceCache ResCache = default!;

    protected Sheetlet()
    {
        IoCManager.InjectDependencies(this);
    }

    public abstract StyleRule[] GetRules(T sheet, object config);
}
