using System.IO;
using System.Linq;
using Content.Server.GameTicking.Prototypes;
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
        _lobbyBackgrounds = _prototypeManager.EnumeratePrototypes<LobbyBackgroundPrototype>()
            .Select(x => x.Background)
            .Where(x => WhitelistedBackgroundExtensions.Contains(x.Extension))
            .ToList();

        LobbyBackground = _lobbyBackgrounds.Any() ? _robustRandom.Pick(_lobbyBackgrounds).ToString() : null;
    }

}
