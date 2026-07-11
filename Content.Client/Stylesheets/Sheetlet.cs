using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;

namespace Content.Client.Stylesheets;

public interface ISheetlet<in T>
{
    StyleRule[] GetRules(T sheet, object config);
}
