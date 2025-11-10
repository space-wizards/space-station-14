using Content.Shared.Administration.Managers.Bwoink;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;

namespace Content.Server.Administration.Managers.Bwoink;

/// <summary>
/// This manager manages the <see cref="CCVars.AdminAhelpOverrideClientName"/> along with the banned / disconnected / re-connected message thingies.
/// </summary>
/// <remarks>
/// Currently the AdminAhelpOverrideClientName is global, and will override the name in all channels. The reason is that I cannot be arsed to actually fix it right nyow (out of scope)
/// </remarks>
public sealed class MessageBwoinkManager
{
    [Dependency] private readonly ServerBwoinkManager _bwoinkManager = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;

    private string _overrideName = string.Empty;

    public void Initialize()
    {
        _bwoinkManager.MessageBeingSent += MessageBeingSent;
        _configurationManager.OnValueChanged(CCVars.AdminAhelpOverrideClientName, s => _overrideName = s);
    }

    private void MessageBeingSent(BwoinkMessageClientSentEventArgs mrrpMeow)
    {
        if (string.IsNullOrWhiteSpace(_overrideName))
            return;

        if (!mrrpMeow.Message.Flags.HasFlag(MessageFlags.Manager))
            return;

        mrrpMeow.Message = mrrpMeow.Message with { Sender = _overrideName };
    }
}
