using System.Text;

using Content.Server.Speech.Components;
using Content.Server.Speech.EntitySystems;
using Content.Server.VoiceMask;
using Content.Server.Wires;
using Content.Shared.Speech;
using Content.Shared.Wires;

namespace Content.Server.Speech;

public sealed partial class ListenWireAction : BaseToggleWireAction
{
    private WiresSystem _wires = default!;

    /// <summary>
    /// Length of the gibberish string sent when pulsing the wire
    /// </summary>
    private int _noiseLength = 16;
    public override Color Color { get; set; } = Color.Green;
    public override string Name { get; set; } = "wire-name-listen";

    public override object? StatusKey { get; } = ListenWireActionKey.StatusKey;

    public override object? TimeoutKey { get; } = ListenWireActionKey.TimeoutKey;

    public override int Delay { get; } = 10;

    public override void Initialize()
    {
        base.Initialize();

        _wires = EntityManager.System<WiresSystem>();
    }
    public override StatusLightState? GetLightState(Wire wire)
    {
        if (GetValue(wire.Owner))
            return StatusLightState.On;
        else
        {
            if (TimeoutKey != null && _wires.HasData(wire.Owner, TimeoutKey))
                return StatusLightState.BlinkingSlow;
            return StatusLightState.Off;
        }
    }
    public override void ToggleValue(EntityUid owner, bool setting)
    {
        if (setting)
        {
            // If we defer removal, the status light gets out of sync
            EntityManager.RemoveComponent<BlockListeningComponent>(owner);
        }
        else
        {
            EntityManager.EnsureComponent<BlockListeningComponent>(owner, out _);
        }
    }

    public override bool GetValue(EntityUid owner)
    {
        return !EntityManager.HasComponent<BlockListeningComponent>(owner);
    }

    public override void Pulse(EntityUid user, Wire wire)
    {
        if (!GetValue(wire.Owner) || !IsPowered(wire.Owner))
            return;

        base.Pulse(user, wire);

        // We have to use a valid euid in the ListenEvent. The user seems
        // like a sensible choice, but we need to mask their name.

        // Save the user's existing voicemask if they have one
        bool oldEnabled = true;
        string oldVoiceName = "ERROR";
        if (EntityManager.TryGetComponent<VoiceMaskComponent>(user, out var oldMask))
        {
            oldEnabled = oldMask.Enabled;
            oldVoiceName = oldMask.VoiceName;
        }

        // Give the user a temporary voicemask component
        var mask = EntityManager.EnsureComponent<VoiceMaskComponent>(user);
        mask.Enabled = true;
        mask.VoiceName = Loc.GetString("wire-listen-pulse-identifier");

        var chars = Loc.GetString("wire-listen-pulse-characters").ToCharArray();
        var noiseMsg = BuildGibberishString(chars, _noiseLength);

        // Send as a ListenEvent to bypass getting blocked by ListenAttemptEvent
        var ev = new ListenEvent(noiseMsg, user);
        EntityManager.EventBus.RaiseLocalEvent(wire.Owner, ev);

        // Remove the voicemask component, or set it back to what it was before
        if (oldMask == null)
            EntityManager.RemoveComponent(user, mask);
        else
        {
            mask.Enabled = oldEnabled;
            mask.VoiceName = oldVoiceName;
        }
    }

    private string BuildGibberishString(char[] charOptions, int length)
    {
        var rand = new Random();
        var sb = new StringBuilder();
        for (var i = 0; i < length; i++)
        {
            var index = rand.Next() % charOptions.Length;
            sb.Append(charOptions[index]);
        }
        return sb.ToString();
    }
}
