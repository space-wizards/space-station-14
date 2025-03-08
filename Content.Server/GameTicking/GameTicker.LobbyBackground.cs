using Content.Server.GameTicking.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using System.Linq;

namespace Content.Server.GameTicking;

public sealed partial class GameTicker
{
    [ViewVariables]
    public string? LobbyBackgroundImage { get; private set; } // imp edit

    [ViewVariables]
    public string? LobbyBackgroundName { get; private set; } // imp edit

    [ViewVariables]
    public string? LobbyBackgroundArtist { get; private set; } // imp edit

    [ViewVariables]
    private List<LobbyBackgroundPrototype> _lobbyBackgrounds = []; // imp edit

    private static readonly string[] WhitelistedBackgroundExtensions = new string[] {"png", "jpg", "jpeg", "webp"};

    private void InitializeLobbyBackground()
    {
        // imp edit
        foreach (var prototype in _prototypeManager.EnumeratePrototypes<LobbyBackgroundPrototype>())
        {
            if (!WhitelistedBackgroundExtensions.Contains(prototype.Background.Extension))
            {
                _sawmill.Warning($"Lobby background '{prototype.ID}' has an invalid extension '{prototype.Background.Extension}' and will be ignored.");
                continue;
            }

            _lobbyBackgrounds.Add(prototype);
        }

        RandomizeLobbyBackground();
    }

    private void RandomizeLobbyBackground()
    {
        // imp edit
        if (_lobbyBackgrounds.Count > 0)
        {
            var background = _robustRandom.Pick(_lobbyBackgrounds);

            LobbyBackgroundImage = background.Background.ToString();
            LobbyBackgroundName = background.Name;
            LobbyBackgroundArtist = background.Artist;
        }
        else
        {
            LobbyBackgroundImage = null;
            LobbyBackgroundName = null;
            LobbyBackgroundArtist = null;
        }
    }
}
