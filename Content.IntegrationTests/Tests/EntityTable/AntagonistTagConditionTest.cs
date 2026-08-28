#nullable enable
using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.Mind;
using Content.Server.Roles;
using Content.Shared.EntityConditions;
using Content.Shared.EntityConditions.Conditions.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Roles;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.EntityTable;

[TestFixture]
[TestOf(typeof(AntagonistTagCondition))]
[TestOf(typeof(AntagonistTagEntityConditionSystem))]
public sealed class AntagonistTagConditionTest : GameTest
{
    [SidedDependency(Side.Server)]
    private readonly SharedEntityConditionsSystem _conditions = null!;

    [SidedDependency(Side.Server)]
    private readonly MindSystem _minds = null!;

    [SidedDependency(Side.Server)]
    private readonly RoleSystem _roles = null!;

    private const string OnStationAntagRole = "AntagTagTestAntagRoleOnStation";
    private const string UnkillableAntagRole = "AntagTagTestAntagRoleUnkillable";

    [TestPrototypes]
    private const string Prototypes =
        $"""
         - type: antag
           id: AntagTagTestAntagOnStation
           name: roles-antag-generic-solo-antagonist-name
           antagonist: true
           objective: never-shown
           tags:
           - OnStation

         - type: antag
           id: AntagTagTestAntagUnkillable
           name: roles-antag-generic-solo-antagonist-name
           antagonist: true
           objective: never-shown
           tags:
           - OnStation
           - Unkillable

         - type: entity
           parent: BaseMindRole
           id: {OnStationAntagRole}
           components:
           - type: MindRole
             antag: true
             antagPrototype: AntagTagTestAntagOnStation

         - type: entity
           parent: BaseMindRole
           id: {UnkillableAntagRole}
           components:
           - type: MindRole
             antag: true
             antagPrototype: AntagTagTestAntagUnkillable
         """;

    private static readonly HashSet<ProtoId<AntagTagPrototype>> OnStationTags = AntagTags("OnStation");
    private static readonly HashSet<ProtoId<AntagTagPrototype>> UnkillableTags = AntagTags("Unkillable");

    [Test]
    [RunOnSide(Side.Server)]
    public void NonAntag_AlwaysPasses_WhenAllowNonAntags()
    {
        var mind = CreateMind();

        Assert.Multiple(() =>
        {
            // Positive filter (changeling.yml: "Only allow on-station targets.").
            var onStation = Cond(OnStationTags);
            Assert.That(_conditions.TryCondition(mind, onStation), Is.True);

            // Inverted filters still pass non-antags.
            var unkillable = Cond(UnkillableTags, inverted: true);
            Assert.That(_conditions.TryCondition(mind, unkillable), Is.True);
            var onStationInverted = Cond(OnStationTags, inverted: true);
            Assert.That(_conditions.TryCondition(mind, onStationInverted), Is.True);
        });
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void NonAntag_InvertedFlagWithAllowNonAntagFalse_WhenDisallowed()
    {
        var mind = CreateMind();

        Assert.Multiple(() =>
        {
            var onStation = Cond(OnStationTags, allowNonAntags: false);
            Assert.That(_conditions.TryCondition(mind, onStation), Is.False);
            var onStationInverted = Cond(OnStationTags, allowNonAntags: false, inverted: true);
            Assert.That(_conditions.TryCondition(mind, onStationInverted), Is.True);
        });
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void CombinedFilter_MatchesChangelingObjectiveIntent()
    {
        var nonAntag = CreateMind();
        var onStationAntag = CreateMind(OnStationAntagRole);
        var unkillableAntag = CreateMind(UnkillableAntagRole);

        Assert.Multiple(() =>
        {
            Assert.That(PassesAll(nonAntag), Is.True);          
            Assert.That(PassesAll(onStationAntag), Is.True);    
            Assert.That(PassesAll(unkillableAntag), Is.False);  
        });

        bool PassesAll(EntityUid mind)
        {
            var onStation = Cond(OnStationTags);
            var unkillableInverted = Cond(UnkillableTags, inverted: true);
            return _conditions.TryCondition(mind, onStation)
                   && _conditions.TryCondition(mind, unkillableInverted);
        }
    }

    private static HashSet<ProtoId<AntagTagPrototype>> AntagTags(params string[] ids)
        => ids.Select(id => new ProtoId<AntagTagPrototype>(id)).ToHashSet();

    private static AntagonistTagCondition Cond(
        HashSet<ProtoId<AntagTagPrototype>> tags,
        bool allowNonAntags = true,
        bool inverted = false
    ) => new()
    {
        Tags = tags,
        AllowNonAntags = allowNonAntags,
        Inverted = inverted
    };

    private EntityUid CreateMind(string? roleProto = null)
    {
        var mind = _minds.CreateMind(null);

        // Give the mind an owned entity so MindAddRole doesn't log an error about it.
        var owned = SSpawn(null);
        SEntMan.EnsureComponent<MindContainerComponent>(owned);
        _minds.TransferTo(mind.Owner, owned);

        if (roleProto != null)
            _roles.MindAddRole(mind.Owner, roleProto, silent: true);

        return mind.Owner;
    }
}
