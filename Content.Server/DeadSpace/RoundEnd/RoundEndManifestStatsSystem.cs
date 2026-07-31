// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Server.GameTicking.Events;
using Content.Shared.Chat;
using Content.Shared.Damage.Systems;
using Content.Shared.DeadSpace.Arena;
using Content.Shared.FixedPoint;
using Content.Shared.GameTicking;
using Content.Shared.Ghost;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Projectiles;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.DeadSpace.RoundEnd;

public sealed class RoundEndManifestStatsSystem : EntitySystem
{
    [Dependency] private readonly SharedRoleSystem _roles = default!;

    private const int MinQuoteLength = 8;
    private const int MaxQuoteLength = 160;
    private const int SourceParentSearchDepth = 6;

    private readonly Dictionary<EntityUid, string> _lastQuoteByMind = new();
    private readonly Dictionary<EntityUid, ManifestKdaStats> _statsByMind = new();
    private readonly Dictionary<EntityUid, Dictionary<EntityUid, FixedPoint2>> _damageByTarget = new();
    private readonly Dictionary<EntityUid, RoundEndManifestIdentity> _identityByMind = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStarting);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
        SubscribeLocalEvent<EntitySpokeEvent>(OnEntitySpoke);
        SubscribeLocalEvent<RoleAddedEvent>(OnRoleAdded);
        SubscribeLocalEvent<MindContainerComponent, EntityRenamedEvent>(OnMindContainerRenamed);
        SubscribeLocalEvent<MobStateComponent, DamageChangedEvent>(OnDamageChanged, before: [typeof(MobThresholdSystem)]);
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
    }

    public RoundEndManifestStats GetManifestStats(EntityUid mindId)
    {
        _statsByMind.TryGetValue(mindId, out var stats);
        return new RoundEndManifestStats(GetQuote(mindId), stats.Kills, stats.Assists);
    }

    public RoundEndManifestIdentity? GetManifestIdentity(EntityUid mindId)
    {
        return _identityByMind.TryGetValue(mindId, out var identity)
            ? identity
            : null;
    }

    public void EnsureManifestEntry(EntityUid mindId, MindComponent mind)
    {
        if (!IsTrackedPlayerMind(mindId, mind))
            return;

        EnsureManifestIdentity(mindId, mind);
    }

    private void OnRoundStarting(RoundStartingEvent ev)
    {
        Reset();
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        Reset();
    }

    private void Reset()
    {
        _lastQuoteByMind.Clear();
        _statsByMind.Clear();
        _damageByTarget.Clear();
        _identityByMind.Clear();
    }

    private void OnEntitySpoke(EntitySpokeEvent args)
    {
        if (!TryGetPlayerMind(args.Source, out var mindId, out var mind) ||
            !IsCharacterSpeechSource(args.Source, mindId, mind))
        {
            return;
        }

        var quote = SanitizeQuote(args.Message);
        if (quote == null)
            return;

        _lastQuoteByMind[mindId] = quote;
    }

    private void OnRoleAdded(RoleAddedEvent args)
    {
        if (!IsAntagPlayerMind(args.MindId, args.Mind))
            return;

        EnsureManifestEntry(args.MindId, args.Mind);
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        if (!TryGetPlayerMind(args.Mob, out var mindId, out var mind))
            return;

        EnsureManifestIdentity(mindId, mind);
    }

    private void OnMindContainerRenamed(EntityUid uid, MindContainerComponent component, ref EntityRenamedEvent args)
    {
        if (string.IsNullOrWhiteSpace(args.NewName) ||
            !TryGetPlayerMind(uid, out var mindId, out _) ||
            !_identityByMind.TryGetValue(mindId, out var identity) ||
            identity.SourceEntity != uid)
        {
            return;
        }

        _identityByMind[mindId] = identity with { CharacterName = args.NewName };

    }

    private void EnsureManifestIdentity(EntityUid mindId, MindComponent mind)
    {
        if (_identityByMind.TryGetValue(mindId, out var identity))
        {
            if (identity.SourceEntity == null &&
                GetIdentitySourceEntity(mind) is { } lateSource)
            {
                _identityByMind[mindId] = identity with { SourceEntity = lateSource };
            }

            return;
        }

        var source = GetIdentitySourceEntity(mind);
        var characterName = GetIdentityCharacterName(mind, source);
        if (string.IsNullOrWhiteSpace(characterName))
            return;

        _identityByMind[mindId] = new RoundEndManifestIdentity(characterName, source);
    }

    private EntityUid? GetIdentitySourceEntity(MindComponent mind)
    {
        if (mind.CurrentEntity is { } currentEntity && !TerminatingOrDeleted(currentEntity))
            return currentEntity;

        if (TryGetEntity(mind.OriginalOwnedEntity, out var originalEntity) &&
            !TerminatingOrDeleted(originalEntity.Value))
        {
            return originalEntity.Value;
        }

        return null;
    }

    private string? GetIdentityCharacterName(MindComponent mind, EntityUid? source)
    {
        if (!string.IsNullOrWhiteSpace(mind.CharacterName))
            return mind.CharacterName;

        if (source != null)
        {
            var sourceName = Name(source.Value);
            if (!string.IsNullOrWhiteSpace(sourceName))
                return sourceName;
        }

        return null;
    }

    private void OnDamageChanged(EntityUid uid, MobStateComponent component, DamageChangedEvent args)
    {
        if (args.DamageDelta == null)
        {
            if (args.Damageable.TotalDamage == FixedPoint2.Zero)
                _damageByTarget.Remove(uid);

            return;
        }

        if (!TryGetPlayerMind(uid, out var targetMindId, out _))
        {
            _damageByTarget.Remove(uid);
            return;
        }

        var delta = args.DamageDelta.GetTotal();

        if (!args.DamageIncreased)
        {
            if (args.Damageable.TotalDamage == FixedPoint2.Zero)
            {
                _damageByTarget.Remove(uid);
                return;
            }

            ReduceDamageContributors(uid, -delta);
            return;
        }

        if (delta <= FixedPoint2.Zero)
            return;

        if (!TryGetDamageSourceMind(args.Origin, out var sourceMindId, out var sourceMind) ||
            sourceMindId == targetMindId ||
            !IsAntagPlayerMind(sourceMindId, sourceMind))
        {
            return;
        }

        var sourceDamage = _damageByTarget.GetOrNew(uid);
        sourceDamage[sourceMindId] = sourceDamage.GetValueOrDefault(sourceMindId) + delta;
    }

    private void OnMobStateChanged(MobStateChangedEvent args)
    {
        var uid = args.Target;
        if (args.NewMobState != MobState.Dead || args.OldMobState >= args.NewMobState)
            return;

        if (!TryGetPlayerMind(uid, out var targetMindId, out _))
        {
            _damageByTarget.Remove(uid);
            return;
        }

        EntityUid? killerMind = null;
        if (TryGetDamageSourceMind(args.Origin, out var originMindId, out var originMind))
        {
            if (originMindId != targetMindId && IsAntagPlayerMind(originMindId, originMind))
                killerMind = originMindId;
        }
        else if (TryGetLargestAntagContributor(uid, targetMindId, out var largestContributor))
        {
            killerMind = largestContributor;
        }

        if (killerMind == null)
        {
            _damageByTarget.Remove(uid);
            return;
        }

        AddKill(killerMind.Value);
        AddAssists(uid, targetMindId, killerMind.Value);
        _damageByTarget.Remove(uid);
    }

    private void AddKill(EntityUid mindId)
    {
        var stats = _statsByMind.GetValueOrDefault(mindId);
        stats.Kills++;
        _statsByMind[mindId] = stats;
    }

    private void AddAssists(EntityUid target, EntityUid targetMindId, EntityUid killerMindId)
    {
        if (!_damageByTarget.TryGetValue(target, out var sources))
            return;

        foreach (var (sourceMindId, damage) in sources)
        {
            if (damage <= FixedPoint2.Zero ||
                sourceMindId == targetMindId ||
                sourceMindId == killerMindId ||
                !TryComp<MindComponent>(sourceMindId, out var sourceMind) ||
                !IsAntagPlayerMind(sourceMindId, sourceMind))
            {
                continue;
            }

            var stats = _statsByMind.GetValueOrDefault(sourceMindId);
            stats.Assists++;
            _statsByMind[sourceMindId] = stats;
        }
    }

    private bool TryGetLargestAntagContributor(EntityUid target, EntityUid targetMindId, out EntityUid sourceMindId)
    {
        sourceMindId = default;
        if (!_damageByTarget.TryGetValue(target, out var sources))
            return false;

        var largestDamage = FixedPoint2.Zero;
        var found = false;

        foreach (var (candidateMindId, damage) in sources)
        {
            if (damage <= largestDamage ||
                candidateMindId == targetMindId ||
                !TryComp<MindComponent>(candidateMindId, out var candidateMind) ||
                !IsAntagPlayerMind(candidateMindId, candidateMind))
            {
                continue;
            }

            sourceMindId = candidateMindId;
            largestDamage = damage;
            found = true;
        }

        return found;
    }

    private void ReduceDamageContributors(EntityUid target, FixedPoint2 healing)
    {
        if (healing <= FixedPoint2.Zero || !_damageByTarget.TryGetValue(target, out var sources))
            return;

        var totalTrackedDamage = FixedPoint2.Zero;
        foreach (var damage in sources.Values)
        {
            if (damage > FixedPoint2.Zero)
                totalTrackedDamage += damage;
        }

        if (totalTrackedDamage <= healing)
        {
            _damageByTarget.Remove(target);
            return;
        }

        var sourceMindIds = new EntityUid[sources.Count];
        sources.Keys.CopyTo(sourceMindIds, 0);

        foreach (var sourceMindId in sourceMindIds)
        {
            var damage = sources[sourceMindId];
            var reduction = damage / totalTrackedDamage * healing;
            var remaining = damage - reduction;
            if (remaining <= FixedPoint2.Zero)
                sources.Remove(sourceMindId);
            else
                sources[sourceMindId] = remaining;
        }

        if (sources.Count == 0)
            _damageByTarget.Remove(target);
    }

    private string GetQuote(EntityUid mindId)
    {
        if (!_lastQuoteByMind.TryGetValue(mindId, out var quote))
            return Loc.GetString("round-end-summary-window-antag-manifest-quote-fallback");

        return quote;
    }

    private string? SanitizeQuote(string message)
    {
        var quote = FormattedMessage.RemoveMarkupPermissive(message).Trim();
        quote = string.Join(" ", quote.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries));

        if (quote.Length < MinQuoteLength || CountMeaningfulCharacters(quote) < 3)
            return null;

        if (quote.Length > MaxQuoteLength)
            quote = $"{quote[..MaxQuoteLength].TrimEnd()}...";

        return quote;
    }

    private static int CountMeaningfulCharacters(string quote)
    {
        var count = 0;
        foreach (var character in quote)
        {
            if (char.IsLetterOrDigit(character))
                count++;
        }

        return count;
    }

    private bool IsCharacterSpeechSource(EntityUid source, EntityUid mindId, MindComponent mind)
    {
        if (mind.OwnedEntity != source || HasComp<GhostComponent>(source))
            return false;

        return !_identityByMind.TryGetValue(mindId, out var identity) ||
               IsSameManifestCharacter(source, mind, identity);
    }

    private bool IsSameManifestCharacter(EntityUid source, MindComponent mind, RoundEndManifestIdentity identity)
    {
        if (identity.SourceEntity == source)
            return true;

        if (!string.IsNullOrWhiteSpace(mind.CharacterName))
            return string.Equals(mind.CharacterName, identity.CharacterName, StringComparison.Ordinal);

        return string.Equals(Name(source), identity.CharacterName, StringComparison.Ordinal);
    }

    public bool IsManifestCharacter(EntityUid mindId, MindComponent mind, EntityUid source)
    {
        EnsureManifestEntry(mindId, mind);

        return !HasComp<GhostComponent>(source) &&
               !_roles.MindHasRole<GhostRoleMarkerRoleComponent>(mindId) &&
               _identityByMind.TryGetValue(mindId, out var identity) &&
               IsSameManifestCharacter(source, mind, identity);
    }

    private bool TryGetDamageSourceMind(EntityUid? source, out EntityUid mindId, out MindComponent mind)
    {
        mindId = default;
        mind = default!;

        if (source == null)
            return false;

        if (TryGetMind(source.Value, out mindId, out mind))
            return true;

        if (TryGetProjectileSourceMind(source.Value, out mindId, out mind))
            return true;

        var current = source.Value;
        for (var i = 0; i < SourceParentSearchDepth; i++)
        {
            if (!TryComp(current, out TransformComponent? transform))
                return false;

            var parent = transform.ParentUid;
            if (parent == current)
                return false;

            if (TryGetMind(parent, out mindId, out mind))
                return true;

            if (TryGetProjectileSourceMind(parent, out mindId, out mind))
                return true;

            current = parent;
        }

        return false;
    }

    private bool TryGetProjectileSourceMind(EntityUid uid, out EntityUid mindId, out MindComponent mind)
    {
        mindId = default;
        mind = default!;

        if (!TryComp<ProjectileComponent>(uid, out var projectile))
            return false;

        if (projectile.Shooter != null && TryGetMind(projectile.Shooter.Value, out mindId, out mind))
            return true;

        return projectile.Weapon != null && TryGetMind(projectile.Weapon.Value, out mindId, out mind);
    }

    private bool TryGetMind(EntityUid uid, out EntityUid mindId, out MindComponent mind)
    {
        mindId = default;
        mind = default!;

        if (!TryComp<MindContainerComponent>(uid, out var mindContainer) ||
            mindContainer.Mind == null)
        {
            return false;
        }

        var mindEntity = mindContainer.Mind.Value;
        if (!TryComp<MindComponent>(mindEntity, out var mindComponent))
            return false;

        mindId = mindEntity;
        mind = mindComponent;
        return true;
    }

    private bool TryGetPlayerMind(EntityUid uid, out EntityUid mindId, out MindComponent mind)
    {
        if (!TryGetMind(uid, out mindId, out mind) || !IsTrackedPlayerMind(mindId, mind))
            return false;

        return true;
    }

    private bool IsAntagPlayerMind(EntityUid mindId, MindComponent mind)
    {
        return IsTrackedPlayerMind(mindId, mind) && _roles.MindIsAntagonist(mindId);
    }

    private bool IsTrackedPlayerMind(EntityUid mindId, MindComponent mind)
    {
        return IsPlayerMind(mind) && !HasComp<ArenaMindComponent>(mindId);
    }

    private static bool IsPlayerMind(MindComponent mind)
    {
        return mind.UserId != null || mind.OriginalOwnerUserId != null;
    }
}

public readonly record struct RoundEndManifestIdentity(string CharacterName, EntityUid? SourceEntity);
public readonly record struct RoundEndManifestStats(string Quote, int Kills, int Assists);

internal struct ManifestKdaStats
{
    public int Kills;
    public int Assists;
}
