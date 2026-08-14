using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Light.Components;
using Content.Shared.Light.Events;
using Content.Shared.Popups;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Storage.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Light.EntitySystems;

public sealed partial class LightReplacerSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private EntityProviderSystem _provider = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedPoweredLightSystem _poweredLight = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;

    [Dependency] private EntityQuery<LightBulbComponent> _lightBulbQuery = default!;

    [SubscribeLocalEvent]
    private void OnExamined(Entity<LightReplacerComponent> replacer, ref ExaminedEvent args)
    {
        if (!_provider.TryGetEntityCounter(replacer.Owner, out var entities))
            return;

        using (args.PushGroup(nameof(LightReplacerComponent)))
        {
            if (entities.Count == 0)
            {
                args.PushMarkup(Loc.GetString("comp-light-replacer-no-lights"));
                return;
            }
            args.PushMarkup(Loc.GetString("comp-light-replacer-has-lights"));

            foreach (var bulb in entities)
            {
                if (!_prototype.Resolve(bulb.Key, out var bulbPrototype))
                    continue;

                args.PushMarkup(Loc.GetString("comp-light-replacer-light-listing", ("amount", bulb.Value), ("name", bulbPrototype.Name)));
            }
        }
    }

    [SubscribeLocalEvent]
    private void OnUse(Entity<LightReplacerComponent> replacer, ref UseInHandEvent args)
    {
        if (args.Handled || !_provider.TryGetEntityCounter(replacer.Owner, out var entities))
            return;

        args.ApplyDelay = false;

        if (entities.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString("comp-light-replacer-open-empty", ("light-replacer", replacer)), replacer, args.User);
            return;
        }

        args.Handled = true;
        _ui.OpenUi(replacer.Owner, LightReplacerUiKey.Key, args.User);
    }

    [SubscribeLocalEvent]
    private void OnAfterInteract(Entity<LightReplacerComponent> replacer, ref AfterInteractEvent eventArgs)
    {
        if (eventArgs.Handled
            || !eventArgs.CanReach // standard interaction checks
            || eventArgs.Target == null) // behavior will depend on the target type
            return;

        var targetUid = (EntityUid) eventArgs.Target;

        // replace broken light in fixture?
        if (TryComp<PoweredLightComponent>(targetUid, out var fixture))
            eventArgs.Handled = TryReplaceBulb(replacer.AsNullable(), (targetUid, fixture), eventArgs.User);
    }

    [SubscribeLocalEvent]
    private void OnEjectMessage(Entity<LightReplacerComponent> replacer, ref EjectLightTypeMessage args)
    {
        _provider.TryEjectEntities(replacer.Owner, args.LightEntProtoId, out _, user: args.Actor);
    }

    [SubscribeLocalEvent]
    private void OnSwitchMessage(Entity<LightReplacerComponent> replacer, ref SwitchLightTypeMessage args)
    {
        if (args.LightType == LightBulbType.Tube)
            replacer.Comp.ActiveLightTube = args.LightEntProtoId;
        else
            replacer.Comp.ActiveLightBulb = args.LightEntProtoId;
        Dirty(replacer);

        if (!_prototype.Resolve(args.LightEntProtoId, out var prototype))
            return;

        var message = Loc.GetString("comp-light-replacer-switch-light", ("light", prototype.Name));
        _popup.PopupEntity(message, replacer, args.Actor);
    }

    [SubscribeLocalEvent]
    private void OnLightProviderInsertedCheck(Entity<LightBulbComponent> bulb, ref EntityProviderInsertCheckEvent args)
    {
        if (bulb.Comp.State == LightBulbState.Broken)
            args.FailureMessage = Loc.GetString("comp-light-replacer-insert-broken-light");
    }

    /// <summary>
    /// Try to replace a light bulb in <paramref name="fixture"/>
    /// using light replacer. Light fixture should have <see cref="PoweredLightComponent"/>.
    /// </summary>
    /// <param name="replacer">The light replacer used to replace the bulb.</param>
    /// <param name="fixture">The fixture whose light is being replaced.</param>
    /// <param name="userUid">The user who is replacing the light.</param>
    /// <returns>True if successfully replaced light, false otherwise</returns>
    public bool TryReplaceBulb(Entity<LightReplacerComponent?> replacer, Entity<PoweredLightComponent?> fixture, EntityUid? userUid = null)
    {
        if (!Resolve(replacer, ref replacer.Comp)
            || !Resolve(fixture, ref fixture.Comp))
            return false;

        var activeType = fixture.Comp.BulbType == LightBulbType.Tube
            ? replacer.Comp.ActiveLightTube
            : replacer.Comp.ActiveLightBulb;

        // check if light bulb is broken or missing
        var fixtureBulbUid = _poweredLight.GetBulb(fixture, fixture.Comp);
        if (fixtureBulbUid != null)
        {
            if (!_lightBulbQuery.TryComp(fixtureBulbUid.Value, out var fixtureBulb))
                return false;

            var prototype = MetaData(fixtureBulbUid.Value).EntityPrototype;

            if (fixtureBulb.State == LightBulbState.Normal && prototype != null && prototype.ID == activeType)
            {
                _popup.PopupEntity(Loc.GetString("comp-light-replacer-same-light", ("light", fixtureBulbUid)), fixture, userUid, PopupType.Medium);
                return false;
            }
        }

        if (!_provider.TryGetEntity(replacer.Owner, activeType, out var insertedBulb))
        {
            if (userUid == null || !_prototype.Resolve(activeType, out var bulbPrototype))
                return false;

            var msg = Loc.GetString("comp-light-replacer-missing-light",
                ("light-name", bulbPrototype.Name),
                ("light-replacer", replacer));
            _popup.PopupEntity(msg, replacer, userUid.Value);
            return false;
        }

        // insert it into fixture
        var wasReplaced = _poweredLight.ReplaceBulb(fixture, insertedBulb.Value, fixture.Comp);
        if (wasReplaced)
        {
            _audio.PlayPredicted(replacer.Comp.Sound, replacer, userUid);
        }

        return wasReplaced;
    }
}

[Serializable, NetSerializable]
public enum LightReplacerUiKey : byte
{
    Key,
}
