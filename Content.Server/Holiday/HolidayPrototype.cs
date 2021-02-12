#nullable enable
using System;
using Content.Server.Holiday.Celebrate;
using Content.Server.Holiday.Greet;
using Content.Server.Holiday.Interfaces;
using Content.Server.Holiday.ShouldCelebrate;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using YamlDotNet.RepresentationModel;

namespace Content.Server.Holiday
{
    [Prototype("holiday")]
    public class HolidayPrototype : IPrototype, IIndexedPrototype
    {
        [ViewVariables] public string Name { get; private set; } = string.Empty;
        [ViewVariables] public string ID { get; private set; } = string.Empty;
        [ViewVariables] public byte BeginDay { get; set; } = 1;
        [ViewVariables] public Month BeginMonth { get; set; } = Month.Invalid;

        /// <summary>
        ///     Day this holiday will end. Zero means it lasts a single day.
        /// </summary>
        [ViewVariables] public byte EndDay { get; set; } = 0;

        /// <summary>
        ///     Month this holiday will end in. Invalid means it lasts a single month.
        /// </summary>
        [ViewVariables] public Month EndMonth { get; set; } = Month.Invalid;

        [ViewVariables]
        private IHolidayShouldCelebrate _shouldCelebrate = new DefaultHolidayShouldCelebrate();

        [ViewVariables]
        private IHolidayGreet _greet = new DefaultHolidayGreet();

        [ViewVariables]
        private IHolidayCelebrate _celebrate = new DefaultHolidayCelebrate();

        public void LoadFrom(YamlMappingNode mapping)
        {
            var serializer = YamlObjectSerializer.NewReader(mapping);
            ExposeData(serializer);
        }

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => x.ID, "id", string.Empty);
            serializer.DataField(this, x => x.Name, "name", string.Empty);
            serializer.DataField(this, x => x.BeginDay, "beginDay", (byte)1);
            serializer.DataField(this, x => x.BeginMonth, "beginMonth", Month.Invalid);
            serializer.DataField(this, x => x.EndDay, "endDay", (byte)0);
            serializer.DataField(this, x => x.EndMonth, "endMonth", Month.Invalid);
            serializer.DataField(ref _shouldCelebrate, "shouldCelebrate", new DefaultHolidayShouldCelebrate());
            serializer.DataField(ref _greet, "greet", new DefaultHolidayGreet());
            serializer.DataField(ref _celebrate, "celebrate", new DefaultHolidayCelebrate());
        }

        public bool ShouldCelebrate(DateTime date)
        {
            return _shouldCelebrate.ShouldCelebrate(date, this);
        }

        public string Greet()
        {
            return _greet.Greet(this);
        }

        /// <summary>
        ///     Called before the round starts to set up any festive shenanigans.
        /// </summary>
        public void Celebrate()
        {
            _celebrate.Celebrate(this);
        }
    }
}
