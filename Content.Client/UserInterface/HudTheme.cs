using Content.Client.HUD;
using Content.Client.Resources;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface;

public sealed class HudTheme
{
    private const string HudAssetPath = "/Textures/Interface/Themed";
    public string Name => _name;
    private readonly string _name;
    public string ResourcePath => HudAssetPath + "/"+Name+"/";

    public HudTheme(string name)
    {
        _name = name;
    }


    //helper to autoresolve dependencies
    public Texture ResolveTexture(string texturePath)
    {
        return ResolveTexture(IoCManager.Resolve<IResourceCache>(), IoCManager.Resolve<IHudManager>(), texturePath);
    }
    public Texture ResolveTexture(IResourceCache cache, IHudManager hudManager, string texturePath)
    {
        return cache.TryGetResource<TextureResource>( new ResourcePath(ResourcePath + texturePath+".png"), out var texture) ? texture :
            cache.GetTexture(HudAssetPath + "/"+"Default"+"/" + texturePath);
    }
}
