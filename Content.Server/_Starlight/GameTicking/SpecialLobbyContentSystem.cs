using Content.Server.Audio;
using Content.Shared.GameTicking.Components;
using Robust.Shared.GameObjects;

namespace Content.Server.GameTicking;

/// <summary>
/// System that handles special lobby content (music and backgrounds) for game rules.
/// </summary>
public sealed class SpecialLobbyContentSystem : EntitySystem
{
    [Dependency] private readonly ContentAudioSystem _contentAudioSystem = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;

    /// <summary>
    /// Attempts to get special lobby content from a game rule entity.
    /// </summary>
    /// <param name="gameRule">The game rule entity to check</param>
    /// <param name="music">The special music track, if any</param>
    /// <param name="background">The special background image, if any</param>
    /// <returns>True if the game rule has special lobby content</returns>
    public bool TryGetSpecialLobbyContent(EntityUid gameRule, out string? music, out string? background)
    {
        music = null;
        background = null;

        if (!TryComp<SpecialLobbyContentComponent>(gameRule, out var specialContent))
            return false;

        music = specialContent.Music;
        background = specialContent.Background;

        return !string.IsNullOrEmpty(music) || !string.IsNullOrEmpty(background);
    }

    /// <summary>
    /// Sets special lobby content from a game rule entity.
    /// </summary>
    /// <param name="gameRule">The game rule entity to get content from</param>
    /// <returns>True if special content was set</returns>
    public bool SetSpecialLobbyContent(EntityUid gameRule)
    {
        if (!TryGetSpecialLobbyContent(gameRule, out var music, out var background))
            return false;

        // Set special music if specified
        if (!string.IsNullOrEmpty(music))
        {
            _contentAudioSystem.SetLobbyPlaylistWithFirstTrack(music);
        }

        // Set special background if specified
        if (!string.IsNullOrEmpty(background))
        {
            _gameTicker.SetLobbyBackground(background);
        }

        return true;
    }
}
