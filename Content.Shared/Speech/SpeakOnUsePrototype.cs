using Robust.Shared.Prototypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared.Speech;

[Prototype]
public sealed partial class SpeakOnUsePrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The words that will be spoken in chat
    /// </summary>
    public string? Sentence;
}
