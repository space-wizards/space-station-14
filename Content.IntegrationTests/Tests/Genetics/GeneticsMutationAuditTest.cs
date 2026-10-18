using System.Collections.Generic;
using System.Linq;
using System.Text;
using Content.IntegrationTests.Fixtures;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Genetics;
using Content.Shared.Actions.Components;
using Content.Shared.Bed.Sleep;
using Content.Shared.Body;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Genetics;
using Content.Shared.Movement.Components;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;
using Content.Shared.Tag;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Genetics;

[TestFixture]
public sealed class GeneticsMutationAuditTest : GameTest
{
    private static readonly string[] DamageTypes = { "Blunt", "Slash", "Piercing", "Heat", "Cold", "Radiation" };

    private string Snapshot(EntityUid uid, bool includeTimers)
    {
        var builder = new StringBuilder();
        builder.AppendLine(string.Join(",", SEntMan.GetComponents(uid)
            .Select(c => c.GetType().Name)
            .Where(n => n is not ("StatusEffectContainerComponent" or "ScaleVisualsComponent"))
            .OrderBy(n => n)));

        var movement = SEntMan.GetComponent<MovementSpeedModifierComponent>(uid);
        builder.AppendLine($"speed {movement.WalkSpeedModifier:F3} {movement.SprintSpeedModifier:F3}");

        if (SEntMan.TryGetComponent(uid, out ContentEyeComponent? contentEye))
            builder.AppendLine($"zoom {contentEye.TargetZoom}");
        if (SEntMan.TryGetComponent(uid, out EyeComponent? eye))
            builder.AppendLine($"fov {eye.DrawFov}");
        if (SEntMan.TryGetComponent(uid, out Content.Server.Atmos.Components.BarotraumaComponent? baro))
            builder.AppendLine($"pressure {baro.HasImmunity}");
        if (SEntMan.TryGetComponent(uid, out ActionsComponent? actions))
            builder.AppendLine($"actions {actions.Actions.Count}");
        if (SEntMan.TryGetComponent(uid, out TagComponent? tags))
            builder.AppendLine($"tags {string.Join(",", tags.Tags.OrderBy(t => t.Id))}");

        var genome = SEntMan.GetComponent<GeneticMutationComponent>(uid);
        builder.AppendLine(
            $"genome {genome.AppliedScale:F2} {genome.XRayActive} {genome.AdaptiveVisionActive} {genome.AntiGlowActive} {genome.BioluminescenceLight != null}");
        builder.AppendLine(
            $"status {Server.System<StatusEffectsSystem>().HasStatusEffect(uid, "StatusEffectMimeMuted")}");

        var damage = new DamageSpecifier();
        foreach (var type in DamageTypes)
            damage.DamageDict[type] = FixedPoint2.New(100);
        var damageEvent = new DamageModifyEvent(damage);
        SEntMan.EventBus.RaiseLocalEvent(uid, damageEvent);
        builder.AppendLine("damage " + string.Join(",",
            DamageTypes.Select(t => damageEvent.Damage.DamageDict.TryGetValue(t, out var v) ? v.ToString() : "-")));

        var melee = new GetMeleeDamageEvent(uid, new DamageSpecifier(), new List<DamageModifierSet>(), uid);
        SEntMan.EventBus.RaiseEvent(EventSource.Local, ref melee);
        builder.AppendLine($"melee {melee.Damage.GetTotal()}");

        if (includeTimers)
        {
            builder.AppendLine(
                $"timers {genome.EpilepsyTimer > 0} {genome.TouretteTimer > 0} {genome.NarcolepsyTimer > 0} " +
                $"{genome.ParanoiaTimer > 0} {genome.NoisesTimer > 0} {genome.MinorNegativeTimer > 0}");
        }

        return builder.ToString();
    }

    [Test]
    public async Task EveryMutationChangesStateAndRestoresItOnRemoval()
    {
        var report = new StringBuilder();
        var failures = 0;
        var periodicOnly = new HashSet<string>
        {
            "GeneticMutationChameleon",
            "GeneticMutationIronLungs",
            "GeneticMutationNobleGas",
            "GeneticMutationRegeneration",
        };
        var subjects = new List<(string Id, EntityUid Uid, string Before, string BeforeStable, string After, bool Applied)>();
        var genetics = Server.System<SharedGeneticsSystem>();
        _ = Server.System<GeneticMutationEffectsSystem>();
        var mutations = Server.ProtoMan.EnumeratePrototypes<GeneticMutationPrototype>().OrderBy(m => m.ID).ToList();

        await Server.WaitAssertion(() =>
        {
            foreach (var mutation in mutations)
            {
                var subject = SSpawn("MobHuman");
                genetics.EnsureGenome(subject);
                var before = Snapshot(subject, true);
                var beforeStable = Snapshot(subject, false);
                var applied = genetics.TryApplyMutationFromInjector(subject, mutation.ID);
                subjects.Add((mutation.ID, subject, before, beforeStable, Snapshot(subject, true), applied));
            }
        });

        await Server.WaitRunTicks(2);

        var removedResults = new Dictionary<string, bool>();
        await Server.WaitAssertion(() =>
        {
            foreach (var subject in subjects)
                removedResults[subject.Id] = genetics.TryRemoveMutation(subject.Uid, subject.Id);
        });

        await Server.WaitRunTicks(2);

        await Server.WaitAssertion(() =>
        {
            foreach (var subject in subjects)
            {
                var restored = Snapshot(subject.Uid, false);
                var problems = new List<string>();
                if (!subject.Applied)
                    problems.Add("could not be applied");
                if (subject.After == subject.Before && !periodicOnly.Contains(subject.Id))
                    problems.Add("no observable effect after applying");
                if (!removedResults[subject.Id])
                    problems.Add("could not be removed");
                if (restored != subject.BeforeStable)
                {
                    var expected = subject.BeforeStable.Split('\n');
                    var actual = restored.Split('\n');
                    var diff = expected.Zip(actual, (e, a) => e == a ? null : $"expected [{e.Trim()}] got [{a.Trim()}]")
                        .Where(d => d != null);
                    problems.Add("state not restored after removal: " + string.Join(" | ", diff));
                }

                if (problems.Count > 0)
                    failures++;

                report.AppendLine($"{(problems.Count == 0 ? "OK  " : "FAIL")} {subject.Id}: " +
                                  (problems.Count == 0 ? "" : string.Join("; ", problems)));
            }
        });

        TestContext.Out.WriteLine(report.ToString());
        Assert.That(failures, Is.EqualTo(0), report.ToString());
    }

    [Test]
    public async Task PeriodicMutationsActuallyTrigger()
    {
        var report = new StringBuilder();
        var failures = 0;

        await Server.WaitAssertion(() =>
        {
            var genetics = Server.System<SharedGeneticsSystem>();
            var effects = Server.System<GeneticMutationEffectsSystem>();
            var statusEffects = Server.System<StatusEffectsSystem>();
            var damageable = Server.System<DamageableSystem>();

            void Check(string id, Func<EntityUid, GeneticMutationComponent, bool> arrange, Func<EntityUid, GeneticMutationComponent, bool> verify)
            {
                var subject = SSpawn("MobHuman");
                var genome = genetics.EnsureGenome(subject);
                var ok = genetics.TryApplyMutationFromInjector(subject, id) && arrange(subject, genome);
                if (ok)
                    effects.Update(1f);
                ok = ok && verify(subject, genome);
                if (!ok)
                    failures++;
                report.AppendLine($"{(ok ? "OK  " : "FAIL")} {id}");
            }

            Check("GeneticMutationEpilepsy", (_, g) => { g.EpilepsyTimer = 0.001f; return true; },
                (u, _) => SEntMan.HasComponent<KnockedDownComponent>(u));
            Check("GeneticMutationNarcolepsy", (_, g) => { g.NarcolepsyTimer = 0.001f; return true; },
                (u, _) => statusEffects.HasStatusEffect(u, SleepingSystem.StatusEffectForcedSleeping));
            Check("GeneticMutationTourette", (_, g) => { g.TouretteTimer = 0.001f; return true; },
                (_, g) => g.TouretteTimer > 0f);
            Check("GeneticMutationParanoia", (_, g) => { g.ParanoiaTimer = 0.001f; return true; },
                (_, g) => g.ParanoiaTimer > 0f);
            Check("GeneticMutationNoises", (_, g) => { g.NoisesTimer = 0.001f; return true; },
                (_, g) => g.NoisesTimer > 0f);
            Check("GeneticMutationCough", (_, g) => { g.MinorNegativeTimer = 0.001f; return true; },
                (_, g) => g.MinorNegativeTimer > 0f);
            Check("GeneticMutationTwoLeftFeet", (_, g) => { g.MinorNegativeTimer = 0.001f; return true; },
                (u, _) => SEntMan.HasComponent<KnockedDownComponent>(u));
            Check("GeneticMutationSpatialInstability", (_, g) => { g.MinorNegativeTimer = 0.001f; return true; },
                (u, _) => SEntMan.HasComponent<KnockedDownComponent>(u));
            Check("GeneticMutationRegeneration",
                (u, g) =>
                {
                    var damage = new DamageSpecifier();
                    damage.DamageDict["Blunt"] = FixedPoint2.New(20);
                    damageable.TryChangeDamage(u, damage, true);
                    g.RegenerationTimer = 1000f;
                    return true;
                },
                (u, _) => damageable.GetAllDamage(u).GetTotal() < FixedPoint2.New(20));
            Check("GeneticMutationIronLungs",
                (u, _) =>
                {
                    var respirator = SEntMan.GetComponent<RespiratorComponent>(u);
                    Server.System<RespiratorSystem>().UpdateSaturation(u, -respirator.Saturation, respirator);
                    return true;
                },
                (u, _) =>
                {
                    var respirator = SEntMan.GetComponent<RespiratorComponent>(u);
                    return respirator.Saturation >= respirator.MaxSaturation - 0.01f;
                });
            Check("GeneticMutationChameleon", (_, _) => true, (u, g) =>
            {
                effects.Update(1f);
                effects.Update(1f);
                return g.ChameleonInvisible;
            });
        });

        TestContext.Out.WriteLine(report.ToString());
        Assert.That(failures, Is.EqualTo(0), report.ToString());
    }
}
