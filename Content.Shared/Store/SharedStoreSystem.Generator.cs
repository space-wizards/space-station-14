using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Store.Components;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
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

        Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2> currency = new();
        currency.Add(entity.Comp.Currency, entity.Comp.Amount);

        if (TryAddCurrency(currency, args.Used, storeComp))
        {
            Popup.PopupEntity(Loc.GetString(entity.Comp.CollectPopup, ("amount", entity.Comp.Amount), ("currency", Loc.GetString(proto.DisplayName)), ("entity", entity)), entity, args.User);
            entity.Comp.Amount = 0;
            DirtyField(entity, entity.Comp, nameof(StoreCurrencyGeneratorComponent.Amount));
        }

    }

    [SubscribeLocalEvent]
    private void OnStoreVerbs(Entity<StoreCurrencyGeneratorComponent> entity, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (entity.Comp.Amount == 0)
            return;

        if (!TryComp<StoreComponent>(args.User, out var storeComp))
            return;

        if (!_whitelist.CheckBoth(args.User, entity.Comp.Blacklist, entity.Comp.Whitelist))
            return;

        if (!ProtoMan.TryIndex(entity.Comp.Currency, out var proto))
            return;

        var user = args.User;
        var amount = entity.Comp.Amount;
        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString(entity.Comp.Verb),
            Message = Loc.GetString(entity.Comp.VerbDescription, ("amount", entity.Comp.Amount), ("currency", Loc.GetString(proto.DisplayName)), ("entity", entity)),
            Act = () =>
            {
                Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2> currency = new();
                currency.Add(entity.Comp.Currency, entity.Comp.Amount);

                if (TryAddCurrency(currency, user, storeComp))
                {
                    Popup.PopupEntity(Loc.GetString(entity.Comp.CollectPopup, ("amount", entity.Comp.Amount), ("currency", Loc.GetString(proto.DisplayName)), ("entity", entity)), entity, user);
                    entity.Comp.Amount = 0;
                    DirtyField(entity, entity.Comp, nameof(StoreCurrencyGeneratorComponent.Amount));
                }
            },
        });
    }

    [SubscribeLocalEvent]
    private void OnExamined(Entity<StoreCurrencyGeneratorComponent> entity, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

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
}
