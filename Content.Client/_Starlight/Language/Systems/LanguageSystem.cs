using Content.Shared._Starlight.Language;
using Content.Shared._Starlight.Language.Components;
using Content.Shared._Starlight.Language.Events;
using Content.Shared._Starlight.Language.Systems;
using Robust.Client.Player;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Client._Starlight.Language.Systems;

public sealed class LanguageSystem : SharedLanguageSystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    /// <summary>
    ///     Invoked when the Languages of the local player entity change, for use in UI.
    /// </summary>
    public event Action? OnLanguagesChanged;

    public override void Initialize()
    {
        _playerManager.LocalPlayerAttached += NotifyUpdate;
        SubscribeLocalEvent<LanguageSpeakerComponent, ComponentHandleState>(OnHandleState);
    }

    private void OnHandleState(Entity<LanguageSpeakerComponent> ent, ref ComponentHandleState args)
    {
        if (args.Current is not LanguageSpeakerComponent.State state)
            return;

        ent.Comp.CurrentLanguage = state.CurrentLanguage;
        ent.Comp.SpokenLanguages = state.SpokenLanguages;
        ent.Comp.UnderstoodLanguages = state.UnderstoodLanguages;

        if (ent.Owner == _playerManager.LocalEntity)
            NotifyUpdate(ent);
    }

    /// <summary>
    ///     Returns the LanguageSpeakerComponent of the local player entity.
    ///     Will return null if the player does not have an entity, or if the client has not yet received the component state.
    /// </summary>
    public LanguageSpeakerComponent? GetLocalSpeaker()
    {
        return CompOrNull<LanguageSpeakerComponent>(_playerManager.LocalEntity);
    }

    public void RequestSetLanguage(ProtoId<LanguagePrototype> language)
    {
        if (GetLocalSpeaker()?.CurrentLanguage?.Equals(language) == true)
            return;

        RaiseNetworkEvent(new LanguagesSetMessage(language));
    }

    private void NotifyUpdate(EntityUid localPlayer)
    {
        RaiseLocalEvent(localPlayer, new LanguagesUpdateEvent(), broadcast: true);
        OnLanguagesChanged?.Invoke();
    }
}