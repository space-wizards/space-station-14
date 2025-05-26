using System.Linq;
using Content.Shared.Interaction.Events;
using Content.Shared.Light.Components;
using Content.Shared.Popups;
using Content.Shared.Examine;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Light.EntitySystems;

[UsedImplicitly]
public abstract class SharedLightReplacerSystem : EntitySystem
{
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Dependency] protected readonly SharedContainerSystem Container = default!;
    [Dependency] protected readonly SharedPopupSystem PopupSystem = default!;
    [Dependency] private   readonly IGameTiming _timing = default!;
    [Dependency] private   readonly IRobustRandom _random = default!;
    [Dependency] private   readonly SharedTransformSystem _transformSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LightReplacerComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<LightReplacerComponent, UseInHandEvent>(HandleUseInHand);
    }

    private void OnExamined(EntityUid uid, LightReplacerComponent component, ExaminedEvent args)
    {
        using (args.PushGroup(nameof(LightReplacerComponent)))
        {
            if (!component.InsertedBulbs.ContainedEntities.Any())
            {
                args.PushMarkup(Loc.GetString("comp-light-replacer-no-lights"));
                return;
            }

            args.PushMarkup(Loc.GetString("comp-light-replacer-has-lights"));
            var groups = new Dictionary<string, int>();
            var metaQuery = GetEntityQuery<MetaDataComponent>();
            foreach (var bulb in component.InsertedBulbs.ContainedEntities)
            {
                var metaData = metaQuery.GetComponent(bulb);
                groups[metaData.EntityName] = groups.GetValueOrDefault(metaData.EntityName) + 1;
            }

            foreach (var (name, amount) in groups)
            {
                args.PushMarkup(Loc.GetString("comp-light-replacer-light-listing", ("amount", amount), ("name", name)));
            }
        }
    }

    private void HandleUseInHand(EntityUid uid, LightReplacerComponent component, UseInHandEvent eventArgs)
    {
        if (eventArgs.Handled)
            return;

        TryEjectBulb(uid, eventArgs.User, true, true);

        eventArgs.Handled = true;
    }

    /// <summary>
    ///     Tries to eject a bulb from storage and onto the floor.
    /// </summary>
    /// <returns>
    ///     Returns true if storage contained at least one light bulb and was able to eject it.
    ///     False otherwise.
    /// </returns>
    private bool TryEjectBulb(
        Entity<LightReplacerComponent?> ent,
        EntityUid? userUid = null,
        bool showPopup = true,
        bool playSound = true)
    {
        if (!TryComp<LightReplacerComponent>(ent, out var replacer))
            return false;

        if (replacer.InsertedBulbs.Count <= 0)
        {
            if (showPopup && userUid != null)
            {
                var msg = Loc.GetString("comp-light-replacer-missing-light", ("light-replacer", ent));
                PopupSystem.PopupEntity(msg, ent, userUid.Value, PopupType.Medium);
            }
            return false;
        }

        // take the bulb out of the container
        var bulbUid = replacer.InsertedBulbs.ContainedEntities.First();
        Container.Remove(bulbUid, replacer.InsertedBulbs);

        // eject the bulb on the ground
        // we use the netEntity here since this is predicted and it allows us to have consistent RNG
        if (TryGetNetEntity(ent, out var netEntity))
            _random.SetSeed((int) _timing.CurTick.Value + netEntity.Value.Id);

        var offsetPos = _random.NextVector2(replacer.EjectOffset);
        var xform = Transform(bulbUid);

        var coordinates = xform.Coordinates;
        coordinates = coordinates.Offset(offsetPos);

        _transformSystem.SetLocalRotation(bulbUid, _random.NextAngle());
        _transformSystem.SetCoordinates(bulbUid, coordinates);

        // play the sound
        if (playSound && TryComp<LightBulbComponent>(bulbUid, out var bulb))
        {
            Audio.PlayPvs(replacer.CycleSound, ent);
            Audio.PlayPvs(bulb.DropSound, bulbUid);
        }

        // show the popup
        if (showPopup && userUid != null)
        {
            var msg = Loc.GetString("comp-light-replacer-eject-light", ("bulb", bulbUid));
            PopupSystem.PopupEntity(msg, ent, userUid.Value, PopupType.Medium);
        }
        return true;
    }
}
