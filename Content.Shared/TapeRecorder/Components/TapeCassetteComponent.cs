using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared.TapeRecorder.Components;

[RegisterComponent]
public sealed partial class TapeCassetteComponent : Component
{
    public List<TapeCassetteRecordedMessage> RecordedData { get; set; } = new List<TapeCassetteRecordedMessage>();

    public float CurrentPosition { get; set; } = 0f;

    [DataField("MaxCapacity")]
    public float MaxCapacity { get; set; } = 120f;
}
