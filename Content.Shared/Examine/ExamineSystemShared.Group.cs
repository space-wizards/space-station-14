using Content.Shared.Ghost;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Shared.Examine
{
    public abstract partial class ExamineSystemShared : EntitySystem
    {
        [Dependency] private readonly IComponentFactory _componentFactory = default!;

        public const string DefaultIconTexture = "/Textures/Interface/examine-star.png";

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GroupExamineComponent, GetVerbsEvent<ExamineVerb>>(OnGroupExamineVerb);

            _ghostQuery = GetEntityQuery<GhostComponent>();
        }

        /// <summary>
        ///     Called when getting verbs on an object with the GroupExamine component. <br/>
        ///     This checks if any of the ExamineGroups are relevant (has 1 or more of the relevant components on the entity)
        ///     and if so, creates an ExamineVerb details button for the ExamineGroup.
        /// </summary>
        private void OnGroupExamineVerb(EntityUid uid, GroupExamineComponent component, GetVerbsEvent<ExamineVerb> args)
        {
            foreach (var group in component.Group)
            {
                if (!EntityHasComponent(uid, group.Components))
                    continue;

                var examineVerb = new ExamineVerb()
                {
                    Act = () =>
                    {
                        SendExamineGroup(args.User, args.Target, group);
                        group.Entries.Clear();
                    },
                    Text = Loc.GetString(group.ContextText),
                    Message = Loc.GetString(group.HoverMessage),
                    Category = VerbCategory.Examine,
                    Icon = group.Icon,
                };

                args.Verbs.Add(examineVerb);
            }
        }

        /// <summary>
        ///     Checks if the entity <paramref name="uid"/> has any of the listed <paramref name="components"/>.
        /// </summary>
        public bool EntityHasComponent(EntityUid uid, List<string> components)
        {
            foreach (var comp in components)
            {
                if (!_componentFactory.TryGetRegistration(comp, out var componentRegistration))
                    continue;

                if (!HasComp(uid, componentRegistration.Type))
                    continue;

                return true;
            }
            return false;
        }

        /// <summary>
        ///     Sends an ExamineTooltip based on the contents of <paramref name="group"/>
        /// </summary>
        public void SendExamineGroup(EntityUid user, EntityUid target, ExamineGroup group)
        {
            var message = new FormattedMessage();

            if (group.Title != null)
            {
                message.AddMarkup(Loc.GetString(group.Title));
                message.PushNewline();
            }
            message.AddMessage(GetFormattedMessageFromExamineEntries(group.Entries));

            SendExamineTooltip(user, target, message, false, false);
        }

        /// <returns>A FormattedMessage based on all <paramref name="entries"/>, sorted.</returns>
        public static FormattedMessage GetFormattedMessageFromExamineEntries(List<ExamineEntry> entries)
        {
            var formattedMessage = new FormattedMessage();
            entries.Sort((a, b) => (b.Priority.CompareTo(a.Priority)));

            var first = true;

            foreach (var entry in entries)
            {
                if (!first)
                {
                    formattedMessage.PushNewline();
                }
                else
                {
                    first = false;
                }

                formattedMessage.AddMessage(entry.Message);
            }

            return formattedMessage;
        }

        /// <summary>
        ///     Either sends the details to a GroupExamineComponent if it finds one, or adds a details examine verb itself.
        /// </summary>
        public void AddDetailedExamineVerb(GetVerbsEvent<ExamineVerb> verbsEvent, Component component, List<ExamineEntry> entries, string verbText, string iconTexture = DefaultIconTexture, string hoverMessage = "")
        {
            // If the entity has the GroupExamineComponent
            if (TryComp<GroupExamineComponent>(verbsEvent.Target, out var groupExamine))
            {
                // Make sure we have the component name as a string
                var componentName = _componentFactory.GetComponentName(component.GetType());

                foreach (var examineGroup in groupExamine.Group)
                {
                    // If any of the examine groups list of components contain this componentname
                    if (examineGroup.Components.Contains(componentName))
                    {
                        foreach (var entry in examineGroup.Entries)
                        {
                            // If any of the entries already are from your component, dont do anything else - no doubles!
                            if (entry.Component == componentName)
                                return;
                        }

                        foreach (var entry in entries)
                        {
                            // Otherwise, just add all information to the examine groups entries, and stop there.
                            examineGroup.Entries.Add(entry);
                        }
                        return;
                    }
                }
            }

            var formattedMessage = GetFormattedMessageFromExamineEntries(entries);

            var examineVerb = new ExamineVerb()
            {
                Act = () =>
                {
                    SendExamineTooltip(verbsEvent.User, verbsEvent.Target, formattedMessage, false, false);
                },
                Text = verbText,
                Message = hoverMessage,
                Category = VerbCategory.Examine,
                Icon = new SpriteSpecifier.Texture(new(iconTexture)),
            };

            verbsEvent.Verbs.Add(examineVerb);
        }

        /// <summary>
        ///     Either adds a details examine verb, or sends the details to a GroupExamineComponent if it finds one.
        /// </summary>
        public void AddDetailedExamineVerb(GetVerbsEvent<ExamineVerb> verbsEvent, Component component, ExamineEntry entry, string verbText, string iconTexture = DefaultIconTexture, string hoverMessage = "")
        {
            AddDetailedExamineVerb(verbsEvent, component, new List<ExamineEntry> { entry }, verbText, iconTexture, hoverMessage);
        }

        /// <summary>
        ///     Either adds a details examine verb, or sends the details to a GroupExamineComponent if it finds one.
        /// </summary>
        public void AddDetailedExamineVerb(GetVerbsEvent<ExamineVerb> verbsEvent, Component component, FormattedMessage message, string verbText, string iconTexture = DefaultIconTexture, string hoverMessage = "")
        {
            var componentName = _componentFactory.GetComponentName(component.GetType());
            AddDetailedExamineVerb(verbsEvent, component, new ExamineEntry(componentName, 0f, message), verbText, iconTexture, hoverMessage);
        }
    }
}
