using Content.Server.GameObjects.Components.Interactable;
using Content.Server.Utility;
using Content.Shared.GameObjects.Components;
using Content.Shared.GameObjects.Components.Interactable;
using Content.Shared.GameObjects.Verbs;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.Console;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Robust.Server.GameObjects;

namespace Content.Server.GameObjects.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedConfigurationComponent))]
    public class ConfigurationComponent : SharedConfigurationComponent, IInteractUsing
    {
        [ViewVariables] private BoundUserInterface UserInterface => Owner.GetUIOrNull(ConfigurationUiKey.Key);

        [ViewVariables]
        private readonly Dictionary<string, string> _config = new();

        private Regex _validation;

        public override void OnAdd()
        {
            base.OnAdd();
            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += UserInterfaceOnReceiveMessage;
            }
        }

        public override void OnRemove()
        {
            base.OnRemove();
            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage -= UserInterfaceOnReceiveMessage;
            }
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataReadWriteFunction("keys", new List<string>(),
                (list) => FillConfiguration(list, _config, ""),
                () => _config.Keys.ToList());

            serializer.DataReadFunction("validation", "^[a-zA-Z0-9 ]*$", value => _validation = new Regex("^[a-zA-Z0-9 ]*$", RegexOptions.Compiled));
        }

        public string GetConfig(string name)
        {
            return _config.GetValueOrDefault(name);
        }

        protected override void Startup()
        {
            base.Startup();
            UpdateUserInterface();
        }

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (UserInterface == null || !eventArgs.User.TryGetComponent(out IActorComponent actor))
                return false;

            if (!eventArgs.Using.TryGetComponent<ToolComponent>(out var tool))
                return false;

            if (!await tool.UseTool(eventArgs.User, Owner, 0.2f, ToolQuality.Multitool))
                return false;

            OpenUserInterface(actor);
            return true;
        }

        private void UserInterfaceOnReceiveMessage(ServerBoundUserInterfaceMessage serverMsg)
        {
            var message = serverMsg.Message;
            var config = new Dictionary<string, string>(_config);

            if (message is ConfigurationUpdatedMessage msg)
            {
                foreach (var key in config.Keys)
                {
                    var value = msg.Config.GetValueOrDefault(key);

                    if (value == null || _validation != null && !_validation.IsMatch(value) && value != "")
                        continue;

                    _config[key] = value;
                }

                SendMessage(new ConfigUpdatedComponentMessage(config));
            }
         }

        private void UpdateUserInterface()
        {
            UserInterface?.SetState(new ConfigurationBoundUserInterfaceState(_config));
        }

        private void OpenUserInterface(IActorComponent actor)
        {
            UpdateUserInterface();
            UserInterface.Open(actor.playerSession);
            UserInterface.SendMessage(new ValidationUpdateMessage(_validation.ToString()), actor.playerSession);
        }

        private static void FillConfiguration<T>(List<string> list, Dictionary<string, T> configuration, T value){
            for (var index = 0; index < list.Count; index++)
            {
                configuration.Add(list[index], value);
            }
        }

        [Verb]
        public sealed class ConfigureVerb : Verb<ConfigurationComponent>
        {
            protected override void GetData(IEntity user, ConfigurationComponent component, VerbData data)
            {
                var session = user.PlayerSession();
                var groupController = IoCManager.Resolve<IConGroupController>();
                if (session == null || !groupController.CanAdminMenu(session))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                data.Text = Loc.GetString("Open Configuration");
                data.IconTexture = "/Textures/Interface/VerbIcons/settings.svg.96dpi.png";
            }

            protected override void Activate(IEntity user, ConfigurationComponent component)
            {
                if (user.TryGetComponent(out IActorComponent actor))
                {
                    component.OpenUserInterface(actor);
                }
            }
        }
    }
}
