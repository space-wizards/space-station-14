using System;
using Content.Server.Act;
using Content.Server.Administration.Logs;
using Content.Server.Popups;
using Content.Shared.Administration.Logs;
using Content.Shared.Audio;
using Content.Shared.Database;
using Content.Shared.Popups;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Stunnable
{
    public sealed class StunSystem : SharedStunSystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly AdminLogSystem _adminLogSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<StatusEffectsComponent, DisarmedActEvent>(OnDisarmed);
        }

        private void OnDisarmed(EntityUid uid, StatusEffectsComponent status, DisarmedActEvent args)
        {
            if (args.Handled || !_random.Prob(args.PushProbability))
                return;

            if (!TryParalyze(uid, TimeSpan.FromSeconds(4f), status))
                return;

            var source = args.Source;
            var target = args.Target;

            if (source != null)
            {
                var knock = EntityManager.GetComponent<KnockedDownComponent>(uid);
                SoundSystem.Play(Filter.Pvs(source), knock.StunAttemptSound.GetSound(), source, AudioHelpers.WithVariation(0.025f));

                if (target != null)
                {
                    // TODO: Use PopupSystem
                    source.PopupMessageOtherClients(Loc.GetString("stunned-component-disarm-success-others", ("source", Name: IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(source.Uid).EntityName), ("target", Name: IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(target.Uid).EntityName)));
                    source.PopupMessageCursor(Loc.GetString("stunned-component-disarm-success", ("target", Name: IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(target.Uid).EntityName)));
                }
            }

            _adminLogSystem.Add(LogType.DisarmedKnockdown, LogImpact.Medium, $"{args.Source:performer} knocked down {args.Target:target}");

            args.Handled = true;
        }
    }
}
