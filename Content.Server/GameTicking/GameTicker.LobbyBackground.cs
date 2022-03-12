using System.IO;
using System.Linq;
using Content.Shared.Audio;
using Robust.Shared.ContentPack;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;


namespace Content.Server.GameTicking;

public sealed partial class GameTicker
{
    [Dependency] private readonly IResourceManager _resourceManager = default!;

    [ViewVariables]
    public string? LobbyBackground { get; private set; }

    [ViewVariables]
    private List<ResourcePath>? _lobbyBackgrounds;

    private static readonly string[] WhitelistedBackgroundExtensions = new string[] {"png", "jpg", "jpeg"};

    private const string LobbyScreenPath = "/Textures/LobbyScreens";

    private void InitializeLobbyBackground()
    {
        _lobbyBackgrounds = _resourceManager.ContentFindFiles(new ResourcePath(LobbyScreenPath)).ToList();
        _lobbyBackgrounds = _lobbyBackgrounds.FindAll(path => WhitelistedBackgroundExtensions.Contains((path.Extension)));
        LobbyBackground = _lobbyBackgrounds.Any() ? _robustRandom.Pick(_lobbyBackgrounds).ToString() : null;
    }

}
