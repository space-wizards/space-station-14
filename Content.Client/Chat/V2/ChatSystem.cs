using Content.Shared.Administration.Managers;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Configuration;

namespace Content.Client.Chat.V2;

public sealed partial class ChatSystem
{
    [Dependency] private readonly ISharedAdminManager _admin = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
}
