using Content.Client.Resources;
using JetBrains.Annotations;
using Robust.Client.ResourceManagement;

namespace Content.Client.UserInterface.XamlExtensions;


[PublicAPI]
public sealed class TexExtension
{
    private IClientResourceCache _resourceCache;
    public string Path { get; }

    public TexExtension(string path)
    {
        _resourceCache = IoCManager.Resolve<IClientResourceCache>();
        Path = path;
    }

    public object ProvideValue()
    {
        return _resourceCache.GetTexture(Path);
    }
}
