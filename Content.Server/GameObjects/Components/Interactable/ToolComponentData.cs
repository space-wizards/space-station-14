using System;
using System.Collections.Generic;
using Content.Shared.GameObjects.Components.Interactable;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.GameObjects.Components.Interactable
{
    public partial class ToolComponentData
    {
        [DataClassTarget("qualities")]
        public ToolQuality Qualities = ToolQuality.None;

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataReadWriteFunction(
                "qualities",
                new List<ToolQuality>(),
                qualities => qualities.ForEach(q => Qualities |= q),
                () =>
                {
                    var qualities = new List<ToolQuality>();

                    foreach (ToolQuality quality in Enum.GetValues(typeof(ToolQuality)))
                    {
                        if ((Qualities & quality) != 0)
                        {
                            qualities.Add(quality);
                        }
                    }

                    return qualities;
                });
        }
    }
}
