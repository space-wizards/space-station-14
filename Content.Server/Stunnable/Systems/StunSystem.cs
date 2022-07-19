using Content.Server.Administration.Logs;
using Content.Server.CombatMode;
using Content.Server.Popups;
using Content.Shared.Audio;
using Content.Shared.Database;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Stunnable
{
    public sealed class StunSystem : SharedStunSystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<StatusEffectsComponent, DisarmedEvent>(OnDisarmed);
        }

        private void OnDisarmed(EntityUid uid, StatusEffectsComponent status, DisarmedEvent args)
        {
            if (args.Handled || !_random.Prob(args.PushProbability))
                return;

            if (!TryParalyze(uid, TimeSpan.FromSeconds(4f), true, status))
                return;

            var source = args.Source;
            var target = args.Target;

            var knock = EntityManager.GetComponent<KnockedDownComponent>(uid);
            SoundSystem.Play(knock.StunAttemptSound.GetSound(), Filter.Pvs(source), source, AudioHelpers.WithVariation(0.025f));

            var targetEnt = Identity.Entity(target, EntityManager);
            var sourceEnt = Identity.Entity(source, EntityManager);
            // TODO: Use PopupSystem
            source.PopupMessageOtherClients(Loc.GetString("stunned-component-disarm-success-others", ("source", sourceEnt), ("target", targetEnt)));
            source.PopupMessageCursor(Loc.GetString("stunned-component-disarm-success", ("target", targetEnt)));

            _adminLogger.Add(LogType.DisarmedKnockdown, LogImpact.Medium, $"{ToPrettyString(args.Source):user} knocked down {ToPrettyString(args.Target):target}");

            args.Handled = true;
        }
    }
}
