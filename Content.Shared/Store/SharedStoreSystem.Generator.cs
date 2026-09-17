using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Store.Components;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Store;

/// <summary>
/// This handles...
/// </summary>
public abstract partial class SharedStoreSystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    [SubscribeLocalEvent]
    private void OnGeneratorInitialize(Entity<StoreCurrencyGeneratorComponent> entity, ref MapInitEvent args)
    {
        entity.Comp.NextGenerationTime = _timing.CurTime + entity.Comp.GenerationDelay;
        DirtyField(entity, entity.Comp, nameof(StoreCurrencyGeneratorComponent.NextGenerationTime));
    }

    [SubscribeLocalEvent]
    private void OnInteractedUsingStore(Entity<StoreCurrencyGeneratorComponent> entity, ref InteractUsingEvent args)
    {
        if (entity.Comp.Amount == 0)
            return;

        if (!TryComp<StoreComponent>(args.Used, out var storeComp))
            return;

        if (!_whitelist.CheckBoth(args.Used, entity.Comp.Blacklist, entity.Comp.Whitelist))
            return;

        if (!ProtoMan.TryIndex(entity.Comp.Currency, out var proto))
            return;

        CollectGenerator(entity, (args.Used, storeComp), args.User, proto);

    }

    [SubscribeLocalEvent]
    private void OnStoreVerbs(Entity<StoreCurrencyGeneratorComponent> entity, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!TryComp<StoreComponent>(args.User, out var storeComp))
            return;

        if (!_whitelist.CheckBoth(args.User, entity.Comp.Blacklist, entity.Comp.Whitelist))
            return;

        if (!ProtoMan.TryIndex(entity.Comp.Currency, out var proto))
            return;

        var user = args.User;
        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString(entity.Comp.Verb),
            Message = entity.Comp.Amount == 0 ? Loc.GetString(entity.Comp.VerbDescriptionEmpty) : Loc.GetString(entity.Comp.VerbDescription, ("amount", entity.Comp.Amount), ("currency", Loc.GetString(proto.DisplayName)), ("entity", entity)),
            Disabled = entity.Comp.Amount == 0, // Dont allow collection when empty
            DoContactInteraction = true,
            Act = () =>
            {
                if (entity.Comp.Amount == 0)
                    return;

                CollectGenerator(entity, (user, storeComp), user, proto);
            },
        });
    }

    [SubscribeLocalEvent]
    private void OnExamined(Entity<StoreCurrencyGeneratorComponent> entity, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        // If we only wanna show the examine message to valid stores.
        if (entity.Comp.StoreExaminable)
        {
            if (!HasComp<StoreComponent>(args.Examiner))
                return;

            if (!_whitelist.CheckBoth(args.Examiner, entity.Comp.Blacklist, entity.Comp.Whitelist))
                return;
        }

        if (!ProtoMan.TryIndex(entity.Comp.Currency, out var proto))
            return;

        args.PushMarkup(Loc.GetString("store-generator-examine", ("amount", entity.Comp.Amount), ("currency", Loc.GetString(proto.DisplayName)), ("entity", entity)));
    }

    private void UpdateGenerator(float frameTime)
    {
        var query = EntityQueryEnumerator<StoreCurrencyGeneratorComponent>();

        var curTime = _timing.CurTime;

        while (query.MoveNext(out var uid, out var generator))
        {
            if (generator.NextGenerationTime > curTime)
                return;

            generator.NextGenerationTime += generator.GenerationDelay;
            DirtyField(uid, generator, nameof(StoreCurrencyGeneratorComponent.NextGenerationTime));

            if (!generator.Enabled)
                return;

            if (generator.Amount >= generator.MaxAmount)
                return;

            generator.Amount += generator.GeneratedAmount;
            DirtyField(uid, generator, nameof(StoreCurrencyGeneratorComponent.Amount));
        }
    }

    private void CollectGenerator(Entity<StoreCurrencyGeneratorComponent> generator, Entity<StoreComponent> collector, EntityUid user, CurrencyPrototype proto)
    {
        Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2> currency = new();
        currency.Add(proto.ID, generator.Comp.Amount);

        if (TryAddCurrency(currency, collector, collector.Comp))
        {
            Popup.PopupEntity(Loc.GetString(generator.Comp.CollectPopup, ("amount", generator.Comp.Amount), ("currency", Loc.GetString(proto.DisplayName)), ("entity", generator)), generator, user);
            generator.Comp.Amount = 0;
            DirtyField(generator, generator.Comp, nameof(StoreCurrencyGeneratorComponent.Amount));
            _audio.PlayPredicted(generator.Comp.CollectSound, generator, user);
        }
    }
}
