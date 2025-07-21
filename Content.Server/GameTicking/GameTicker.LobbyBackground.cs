using Content.Server.GameTicking.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using System.Linq;

namespace Content.Server.GameTicking;

public sealed partial class GameTicker
{
    [ViewVariables]
    public string? LobbyBackground { get; private set; }

    [ViewVariables]
    private List<ResPath>? _lobbyBackgrounds;

    // STARLIGHT: Support for conditional lobby backgrounds
    private string? _forcedLobbyBackground;

    private static readonly string[] WhitelistedBackgroundExtensions = new string[] {"png", "jpg", "jpeg", "webp"};

    private void InitializeLobbyBackground()
    {
        _lobbyBackgrounds = _prototypeManager.EnumeratePrototypes<LobbyBackgroundPrototype>()
            .Select(x => x.Background)
            .Where(x => WhitelistedBackgroundExtensions.Contains(x.Extension))
            .ToList();

        RandomizeLobbyBackground();
    }

    private void RandomizeLobbyBackground() {
        // STARLIGHT: Check if we have a forced background first
        if (_forcedLobbyBackground != null)
        {
            LobbyBackground = _forcedLobbyBackground;
            _forcedLobbyBackground = null; // Reset after use
            return;
        }
        
        LobbyBackground = _lobbyBackgrounds!.Any() ? _robustRandom.Pick(_lobbyBackgrounds!).ToString() : null;
    }

    /// <summary>
    /// STARLIGHT: Sets a specific lobby background to be used on the next round restart.
    /// </summary>
    /// <param name="backgroundPath">The path to the background image</param>
    public void SetLobbyBackground(string backgroundPath)
    {
        _forcedLobbyBackground = backgroundPath;
    }
}
