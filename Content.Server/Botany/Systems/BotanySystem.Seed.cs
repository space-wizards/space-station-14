using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.Botany.Components;
using Content.Shared.Examine;
using Content.Shared.Random.Helpers;
using Content.Shared.Tag;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Botany.Systems;

public sealed partial class BotanySystem
{
    public void InitializeSeeds()
    {
        SubscribeLocalEvent<SeedComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(EntityUid uid, SeedComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (!_prototypeManager.TryIndex<SeedPrototype>(component.SeedName, out var seed))
            return;

        args.PushMarkup(Loc.GetString($"seed-component-description", ("seedName", seed.DisplayName)));

        if (!seed.RoundStart)
        {
            args.PushMarkup(Loc.GetString($"seed-component-has-variety-tag", ("seedUid", seed.Uid)));
        }
        else
        {
            args.PushMarkup(Loc.GetString($"seed-component-plant-yield-text", ("seedYield", seed.Yield)));
            args.PushMarkup(Loc.GetString($"seed-component-plant-potency-text", ("seedPotency", seed.Potency)));
        }
    }

    #region SeedPrototype prototype stuff

    public EntityUid SpawnSeedPacket(SeedPrototype proto, EntityCoordinates transformCoordinates)
    {
        var seed = Spawn(SeedPrototype.Prototype, transformCoordinates);

        var seedComp = EnsureComp<SeedComponent>(seed);
        seedComp.SeedName = proto.ID;

        if (TryComp(seed, out SpriteComponent? sprite))
        {
            // TODO visualizer
            // SeedPrototype state will always be seed. Blame the spriter if that's not the case!
            sprite.LayerSetSprite(0, new SpriteSpecifier.Rsi(proto.PlantRsi, "seed"));
        }

        string val = Loc.GetString("botany-seed-packet-name", ("seedName", proto.SeedName), ("seedNoun", proto.SeedNoun));
        MetaData(seed).EntityName = val;

        return seed;
    }

    public IEnumerable<EntityUid> AutoHarvest(SeedPrototype proto, EntityCoordinates position, int yieldMod = 1)
    {
        if (position.IsValid(EntityManager) &&
            proto.ProductPrototypes.Count > 0)
            return GenerateProduct(proto, position, yieldMod);

        return Enumerable.Empty<EntityUid>();
    }

    public IEnumerable<EntityUid> Harvest(SeedPrototype proto, EntityUid user, int yieldMod = 1)
    {
        if (AddSeedToDatabase(proto)) proto.Name = proto.Uid.ToString();

        if (proto.ProductPrototypes.Count == 0 || proto.Yield <= 0)
        {
            _popupSystem.PopupCursor(Loc.GetString("botany-harvest-fail-message"),
                Filter.Entities(user));
            return Enumerable.Empty<EntityUid>();
        }

        _popupSystem.PopupCursor(Loc.GetString("botany-harvest-success-message", ("name", proto.DisplayName)),
            Filter.Entities(user));
        return GenerateProduct(proto, Transform(user).Coordinates, yieldMod);
    }

    public IEnumerable<EntityUid> GenerateProduct(SeedPrototype proto, EntityCoordinates position, int yieldMod = 1)
    {
        var totalYield = 0;
        if (proto.Yield > -1)
        {
            if (yieldMod < 0)
            {
                yieldMod = 1;
                totalYield = proto.Yield;
            }
            else
                totalYield = proto.Yield * yieldMod;

            totalYield = Math.Max(1, totalYield);
        }

        var products = new List<EntityUid>();

        for (var i = 0; i < totalYield; i++)
        {
            var product = _robustRandom.Pick(proto.ProductPrototypes);

            var entity = Spawn(product, position);
            entity.RandomOffset(0.25f);
            products.Add(entity);

            var produce = EnsureComp<ProduceComponent>(entity);

            produce.SeedName = proto.ID;
            ProduceGrown(entity, produce);

            if (proto.Mysterious)
            {
                var metaData = MetaData(entity);
                metaData.EntityName += "?";
                metaData.EntityDescription += " " + Loc.GetString("botany-mysterious-description-addon");
            }
        }

        return products;
    }

    public bool CanHarvest(SeedPrototype proto, EntityUid? held = null)
    {
        return !proto.Ligneous || proto.Ligneous && held != null && _tags.HasTag(held.Value, "BotanySharp");
    }

    #endregion
}
