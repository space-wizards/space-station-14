using System.Linq;
using System.Threading;
using Content.Server.DoAfter;
using Content.Shared.Damage;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.Tools.Components;
using Content.Shared.Weapons.Melee;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.TapeRecorder
{
    public sealed class CassetteTapeSystem : EntitySystem
    {
        [Dependency] private readonly TagSystem _tagSystem = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;
        [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
        [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;

        /// <inheritdoc/>
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<CassetteTapeComponent, DamageChangedEvent>(OnDamagedChanged);
            SubscribeLocalEvent<CassetteTapeComponent, InteractUsingEvent>(OnInteractUsing);
            SubscribeLocalEvent<CassetteTapeComponent, SpoolingCompleteEvent>(OnSpoolingComplete);
            SubscribeLocalEvent<CassetteTapeComponent, SpoolingCancelEvent>(OnSpoolingCancel);
        }


        /// <summary>
        /// Handles "fixing" the cassette tape by screwing it back in with a screwdriver (ADD SUPPORT FOR PENCILS WHEN THOSE ARE ADDED)
        /// </summary>
        private void OnInteractUsing(EntityUid uid, CassetteTapeComponent component, InteractUsingEvent args)
        {
            if (!component.Unspooled || component.CancelToken != null)
                return;
            if (!TryComp<ToolComponent>(args.Used, out var toolComponent))
                return;
            if (!toolComponent.Qualities.Contains("Screwing"))
                return;

            component.CancelToken = new CancellationTokenSource();

            var doAfterEventArgs = new DoAfterEventArgs(args.User, 7, component.CancelToken.Token, component.Owner)
            {
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                BreakOnDamage = true,
                BreakOnStun = true,
                NeedHand = true,
                TargetFinishedEvent = new SpoolingCompleteEvent
                {
                    Component = component,
                    User = args.User,
                },
                TargetCancelledEvent = new SpoolingCancelEvent()
            };

            _popupSystem.PopupEntity(Loc.GetString("cassette-repair-start", ("item", component.Owner)), args.User, Filter.Entities(args.User));
            _doAfterSystem.DoAfter(doAfterEventArgs);
        }

        private void OnSpoolingComplete(EntityUid uid, CassetteTapeComponent component, SpoolingCompleteEvent args)
        {
            _popupSystem.PopupEntity(Loc.GetString("cassette-repair-finish", ("item", component.Owner)), args.User, Filter.Entities(args.User));

            _tagSystem.AddTag(component.Owner, "CassetteTape");
            if (!TryComp<SpriteComponent>(component.Owner, out var spriteComponent))
                return;
            spriteComponent.LayerSetVisible(1, false);
            component.Unspooled = false;
            component.TimeStamp = 0; //may as well rewind it while fixing it
            component.CancelToken = null;
        }

        private void OnSpoolingCancel(EntityUid uid, CassetteTapeComponent component, SpoolingCancelEvent args)
        {
            component.CancelToken = null;
        }

        /// <summary>
        /// Let the tape spaghetti fly out of the tape when it's damaged
        /// </summary>
        private void OnDamagedChanged(EntityUid uid, CassetteTapeComponent component, DamageChangedEvent args)
        {
            if (args.DamageDelta == null || args.DamageDelta.Total < 5)
                return;

            _tagSystem.RemoveTag(component.Owner, "CassetteTape");
            if (!TryComp<SpriteComponent>(component.Owner, out var spriteComponent))
                return;
            spriteComponent.LayerSetVisible(1, true);
            component.Unspooled = true;

            if (component.RecordedMessages.Count == 0)
                return;

            //Erase a random entry in recorded messages, gotta have that tape corruption

            //Lets corrupt a random entry on the tape
            var index = _robustRandom.Next(0, component.RecordedMessages.Count);
            var timestamp = component.RecordedMessages[index].MessageTimeStamp;
            component.RecordedMessages.RemoveAt(index);

            //Generate a random string of characters containing @ # % or ~ in random orders


            var randomString = new string(Enumerable.Repeat("@#$%~", _robustRandom.Next(3, 10)).Select(s => s[_robustRandom.Next(s.Length)]).ToArray());

            //Add the random string to the list
            component.RecordedMessages.Add((timestamp, "(" + TimeSpan.FromSeconds(timestamp).ToString("mm\\:ss") + ") " + randomString));

            //Sort the list
            component.RecordedMessages.Sort((x, y) => x.MessageTimeStamp.CompareTo(y.MessageTimeStamp));

            //component.RecordedMessages.RemoveAt(_robustRandom.Next(0, component.RecordedMessages.Count));
        }


        private sealed class SpoolingCompleteEvent : EntityEventArgs
        {
            public EntityUid User;
            public CassetteTapeComponent Component = default!;
        }
        private sealed class SpoolingCancelEvent : EntityEventArgs
        {
        }
    }

}
