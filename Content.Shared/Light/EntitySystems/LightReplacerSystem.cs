using System.Linq;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Light.Components;
using Content.Shared.Light.Events;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Serialization;

namespace Content.Shared.Light.EntitySystems;

public sealed class LightReplacerSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedPoweredLightSystem _poweredLight = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    private EntityQuery<LightBulbComponent> _lightBulbQuery;
    private EntityQuery<MetaDataComponent> _metaDataQuery;

    public override void Initialize()
    {
        SubscribeLocalEvent<LightReplacerComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<LightReplacerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<LightReplacerComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<LightReplacerComponent, UseInHandEvent>(OnUse);
        SubscribeLocalEvent<LightReplacerComponent, InteractUsingEvent>(HandleInteract);
        SubscribeLocalEvent<LightReplacerComponent, AfterInteractEvent>(HandleAfterInteract);
        SubscribeLocalEvent<LightReplacerComponent, EjectLightTypeMessage>(OnEjectMessage);
        SubscribeLocalEvent<LightReplacerComponent, SwitchLightTypeMessage>(OnSwitchMessage);

        _lightBulbQuery = GetEntityQuery<LightBulbComponent>();
        _metaDataQuery = GetEntityQuery<MetaDataComponent>();
    }

    private void OnInit(Entity<LightReplacerComponent> replacer, ref ComponentInit args)
    {
        // This needs to be handled on CompInit because otherwise, it's empty on the client.
        replacer.Comp.InsertedBulbs = _container.EnsureContainer<Container>(replacer, "light_replacer_storage");
    }

    private void OnMapInit(Entity<LightReplacerComponent> replacer, ref MapInitEvent args)
    {
        var xform = Transform(replacer);
        foreach (var spawn in EntitySpawnCollection.GetSpawns(replacer.Comp.StartingContent))
        {
            var light = Spawn(spawn, xform.Coordinates);
            TryInsertBulb(replacer.AsNullable(), light);
        }
    }

    private void OnExamined(Entity<LightReplacerComponent> replacer, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(LightReplacerComponent)))
        {
            if (!replacer.Comp.InsertedBulbs.ContainedEntities.Any())
            {
                args.PushMarkup(Loc.GetString("comp-light-replacer-no-lights"));
                return;
            }

            args.PushMarkup(Loc.GetString("comp-light-replacer-has-lights"));
            var groups = new Dictionary<string, int>();
            foreach (var bulb in replacer.Comp.InsertedBulbs.ContainedEntities)
            {
                var metaData = _metaDataQuery.GetComponent(bulb);
                groups[metaData.EntityName] = groups.GetValueOrDefault(metaData.EntityName) + 1;
            }

            foreach (var (name, amount) in groups)
            {
                args.PushMarkup(Loc.GetString("comp-light-replacer-light-listing", ("amount", amount), ("name", name)));
            }
        }
    }

    private void OnUse(Entity<LightReplacerComponent> replacer, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.ApplyDelay = false;

        if (!replacer.Comp.InsertedBulbs.ContainedEntities.Any())
        {
            _popup.PopupClient(Loc.GetString("comp-light-replacer-open-empty", ("light-replacer", replacer)), replacer, args.User);
            return;
        }

        args.Handled = true;
        _ui.OpenUi(replacer.Owner, LightReplacerUiKey.Key, args.User);
    }

    private void HandleAfterInteract(Entity<LightReplacerComponent> replacer, ref AfterInteractEvent eventArgs)
    {
        if (eventArgs.Handled
            || !eventArgs.CanReach // standard interaction checks
            || eventArgs.Target == null) // behavior will depend on the target type
            return;

        var targetUid = (EntityUid) eventArgs.Target;

        // replace broken light in fixture?
        if (TryComp<PoweredLightComponent>(targetUid, out var fixture))
            eventArgs.Handled = TryReplaceBulb(replacer.AsNullable(), (targetUid, fixture), eventArgs.User);
        // add new bulb to light replacer container?
        else if (_lightBulbQuery.TryComp(targetUid, out var bulb))
            eventArgs.Handled = TryInsertBulb(replacer.AsNullable(), (targetUid, bulb), eventArgs.User, true);
    }

    private void HandleInteract(Entity<LightReplacerComponent> replacer, ref InteractUsingEvent eventArgs)
    {
        if (eventArgs.Handled)
            return;

        var usedUid = eventArgs.Used;

        // want to insert a new light bulb?
        if (_lightBulbQuery.TryComp(usedUid, out var bulb))
            eventArgs.Handled = TryInsertBulb(replacer.AsNullable(), (usedUid, bulb), eventArgs.User, true);
        // add bulbs from storage?
        else if (TryComp<StorageComponent>(usedUid, out var storage))
            eventArgs.Handled = TryInsertBulbsFromStorage(replacer.AsNullable(), (usedUid, storage), eventArgs.User);
    }

    private void OnEjectMessage(Entity<LightReplacerComponent> replacer, ref EjectLightTypeMessage args)
    {
        HashSet<EntityUid> lightsToEject = [];
        foreach (var light in replacer.Comp.InsertedBulbs.ContainedEntities)
        {
            if (_metaDataQuery.TryComp(light, out var metaData) && metaData.EntityName == args.LightName)
                lightsToEject.Add(light);
        }

        foreach (var light in lightsToEject)
        {
            _container.Remove(light, replacer.Comp.InsertedBulbs);
        }
    }

    private void OnSwitchMessage(Entity<LightReplacerComponent> replacer, ref SwitchLightTypeMessage args)
    {
        if (args.LightType == LightBulbType.Tube)
            replacer.Comp.ActiveLightTube = args.LightName;
        else
            replacer.Comp.ActiveLightBulb = args.LightName;
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

            if (fixtureBulb.State == LightBulbState.Normal
                && _metaDataQuery.TryComp(fixtureBulbUid, out var metaData)
                && metaData.EntityName == activeType)
            {
                _popup.PopupClient(Loc.GetString("comp-light-replacer-same-light", ("light", fixtureBulbUid)), fixture, userUid, PopupType.Medium);
                return false;
            }
        }

        EntityUid? bulb = null;
        foreach (var insertedBulb in replacer.Comp.InsertedBulbs.ContainedEntities)
        {
            if (!_metaDataQuery.TryComp(insertedBulb, out var metaData) || metaData.EntityName != activeType)
                continue;

            bulb = insertedBulb;
            break;
        }

        // found bulb in inserted storage
        if (bulb.HasValue)
        {
            // try to remove it
            var hasRemoved = _container.Remove(bulb.Value, replacer.Comp.InsertedBulbs);
            if (!hasRemoved)
                return false;
        }
        else
        {
            if (userUid == null)
                return false;

            var msg = Loc.GetString("comp-light-replacer-missing-light",
                ("light-name", activeType),
                ("light-replacer", replacer));
            _popup.PopupClient(msg, replacer, userUid.Value);
            return false;
        }

        // insert it into fixture
        var wasReplaced = _poweredLight.ReplaceBulb(fixture, bulb.Value, fixture.Comp);
        if (wasReplaced)
        {
            _audio.PlayPredicted(replacer.Comp.Sound, replacer, userUid);
        }

        return wasReplaced;
    }

    /// <summary>
    /// Try to insert a new bulb inside light replacer
    /// </summary>
    /// <param name="replacer">The light replacer to insert a light into.</param>
    /// <param name="bulb">The light to insert into the replacer.</param>
    /// <param name="userUid">The user who is inserting the light.</param>
    /// <param name="showPopup">Whether to show a popup.</param>
    /// <returns>True if successfully inserted light, false otherwise</returns>
    public bool TryInsertBulb(Entity<LightReplacerComponent?> replacer, Entity<LightBulbComponent?> bulb, EntityUid? userUid = null, bool showPopup = false)
    {
        if (!Resolve(replacer, ref replacer.Comp)
            || !Resolve(bulb, ref bulb.Comp))
            return false;

        // only normal (non-broken) bulbs can be inserted inside light replacer
        if (bulb.Comp.State != LightBulbState.Normal)
        {
            if (!showPopup || userUid == null)
                return false;

            var error = Loc.GetString("comp-light-replacer-insert-broken-light");
            _popup.PopupClient(error, replacer, userUid.Value);
            return false;
        }
        // try insert light and show message
        var hasInsert = _container.Insert(bulb.Owner, replacer.Comp.InsertedBulbs);

        if (!hasInsert || !showPopup || userUid == null)
            return hasInsert;

        var message = Loc.GetString("comp-light-replacer-insert-light",
            ("light-replacer", replacer), ("bulb", bulb));
        _popup.PopupClient(message, replacer, userUid.Value, PopupType.Medium);

        return hasInsert;
    }

    /// <summary>
    /// Try to insert all light bulbs from storage (for example light tubes box)
    /// </summary>
    /// <param name="replacer">The light replacer to insert bulbs into.</param>
    /// <param name="storage">The storage whose contents should be inserted.</param>
    /// <param name="userUid">The user who inserts the contents.</param>
    /// <returns>
    /// Returns true if storage contained at least one light bulb
    /// which was successfully inserted inside light replacer
    /// </returns>
    public bool TryInsertBulbsFromStorage(Entity<LightReplacerComponent?> replacer, Entity<StorageComponent?> storage, EntityUid? userUid = null)
    {
        if (!Resolve(replacer, ref replacer.Comp)
            || !Resolve(storage, ref storage.Comp))
            return false;

        var insertedBulbs = 0;
        var storedEntities = storage.Comp.Container.ContainedEntities.ToArray();

        foreach (var ent in storedEntities)
        {
            if (TryInsertBulb(replacer, ent, userUid))
            {
                insertedBulbs++;
            }
        }

        // show some message if success
        if (insertedBulbs > 0 && userUid != null)
        {
            var msg = Loc.GetString("comp-light-replacer-refill-from-storage", ("light-replacer", replacer));
            _popup.PopupClient(msg, replacer, userUid.Value, PopupType.Medium);
        }

        return insertedBulbs > 0;
    }
}

[Serializable, NetSerializable]
public enum LightReplacerUiKey : byte
{
    Key,
}
