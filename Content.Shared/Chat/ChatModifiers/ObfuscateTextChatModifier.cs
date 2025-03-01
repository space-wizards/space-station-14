using System.Linq;
using System.Text;
using Content.Shared.CCVar;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Decals;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Shared.Chat.ChatModifiers;

/// <summary>
/// Wraps the [EntityNameHeader] tag in a [color] tag, should the player have colored names enabled.
/// </summary>
[Serializable]
[DataDefinition]
public sealed partial class ObfuscateTextChatModifier : ChatModifier
{

    [Dependency] private readonly IRobustRandom _random = default!;

    [DataField]
    public float ObfuscationChance = 0.8f;

    public override void ProcessChatModifier(ref FormattedMessage message, Dictionary<Enum, object> channelParameters)
    {
        IoCManager.InjectDependencies(this);

        if (channelParameters.TryGetValue(DefaultChannelParameters.RandomSeed, out var seed))
        {

            var returnMessage = new FormattedMessage();
            var nodeEnumerator = message.GetEnumerator();

            _random.SetSeed((int)seed);

            while (nodeEnumerator.MoveNext())
            {
                var node = nodeEnumerator.Current;
                if (node.Name == null && node.Value.TryGetString(out var text))
                {
                    returnMessage.AddText(ObfuscateMessageReadability(text, ObfuscationChance));
                }
                else
                {
                    if (!node.Closing)
                        returnMessage.PushTag(node);
                    else
                        returnMessage.Pop();
                }
            }

            nodeEnumerator.Dispose();
            message = returnMessage;
        }
    }

    private string ObfuscateMessageReadability(string message, float chance)
    {
        var charArray = message.ToCharArray();

        for (var i = 0; i < charArray.Length; i++)
        {
            if (char.IsWhiteSpace((charArray[i])))
            {
                continue;
            }

            if (_random.Prob(chance))
            {
                charArray[i] = '~';
            }
        }

        return new string(charArray);
    }
}
