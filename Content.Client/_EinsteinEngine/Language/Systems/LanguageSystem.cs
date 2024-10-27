using Content.Shared._EinsteinEngine.Language;
using Content.Shared._EinsteinEngine.Language.Events;
using Content.Shared._EinsteinEngine.Language.Systems;
using Robust.Client;

namespace Content.Client._EinsteinEngine.Language.Systems;

/// <summary>
///   Client-side language system.
/// </summary>
/// <remarks>
///   Unlike the server, the client is not aware of other entities' languages; it's only notified about the entity that it posesses.
///   Due to that, this system stores such information in a static manner.
/// </remarks>
public sealed class LanguageSystem : SharedLanguageSystem
{
    [Dependency] private readonly IBaseClient _client = default!;

    /// <summary>
    ///   The current language of the entity currently possessed by the player.
    /// </summary>
    public string CurrentLanguage { get; private set; } = default!;
    /// <summary>
    ///   The list of languages the currently possessed entity can speak.
    /// </summary>
    public List<string> SpokenLanguages { get; private set; } = new();
    /// <summary>
    ///   The list of languages the currently possessed entity can understand.
    /// </summary>
    public List<string> UnderstoodLanguages { get; private set; } = new();

    public event EventHandler<LanguagesUpdatedMessage>? OnLanguagesChanged;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<LanguagesUpdatedMessage>(OnLanguagesUpdated);
        _client.RunLevelChanged += OnRunLevelChanged;
    }

    private void OnLanguagesUpdated(LanguagesUpdatedMessage message)
    {
        // TODO this entire thing is horrible. If someone is willing to refactor this, LanguageSpeakerComponent should become shared with SendOnlyToOwner = true
        // That way, this system will be able to use the existing networking infrastructure instead of relying on this makeshift... whatever this is.
        CurrentLanguage = message.CurrentLanguage;
        SpokenLanguages = message.Spoken;
        UnderstoodLanguages = message.Understood;

        OnLanguagesChanged?.Invoke(this, message);
    }

    private void OnRunLevelChanged(object? sender, RunLevelChangedEventArgs args)
    {
        // Request an update when entering a game
        if (args.NewLevel == ClientRunLevel.InGame)
            RequestStateUpdate();
    }

    /// <summary>
    ///   Sends a network request to the server to update this system's state.
    ///   The server may ignore the said request if the player is not possessing an entity.
    /// </summary>
    public void RequestStateUpdate()
    {
        RaiseNetworkEvent(new RequestLanguagesMessage());
    }

    public void RequestSetLanguage(LanguagePrototype language)
    {
        if (language.ID == CurrentLanguage)
            return;

        RaiseNetworkEvent(new LanguagesSetMessage(language.ID));

        // May cause some minor desync...
        // So to reduce the probability of desync, we replicate the change locally too
        if (SpokenLanguages.Contains(language.ID))
            CurrentLanguage = language.ID;
    }
}
