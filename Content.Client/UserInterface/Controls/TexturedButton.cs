using Content.Client.Resources;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.UserInterface.Controls;

public sealed class TexturedButton : TextureButton
{

    [Dependency] private readonly IResourceCache _resourceCache = default!;

    public string? TexturePath
    {
        set
        {
            if (value == null) return;
            TextureNormal = _resourceCache.GetTexture(value);
        }
    }

    public TexturedButton()
    {
        IoCManager.InjectDependencies(this);

    }
}
