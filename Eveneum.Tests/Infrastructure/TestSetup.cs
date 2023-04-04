using System;
using System.Linq;
using NodaTime;
using RandomGen;

namespace Eveneum.Tests.Infrastructure
{
    static class TestSetup
    {
        public static EventData[] GetEvents(int count = 5, int startVersion = 1, string streamId = null)
        {
            streamId = streamId ?? Gen.Random.Text.Words()();
            var numbers = Gen.Random.Numbers.Decimals();
            var strings = Gen.Random.Text.VeryLong();
            var dates = Gen.Random.Time.Dates(DateTime.MinValue);

            return Enumerable.Range(startVersion, count)
                .Select(x => new SampleEvent
                {
                    Version = x,
                    Number = numbers(),
                    LocalDate = LocalDate.FromDateTime(dates()),
                    LocalDateTime = LocalDateTime.FromDateTime(dates()),
                    Nested = new NestedContent
                    {
                        Content = strings()
                    }
                })
                .Select(x => new EventData(streamId, x, GetMetadata(), (ulong)x.Version, numbers().ToString()))
                .ToArray();
        }

        public static SampleMetadata GetMetadata() => new SampleMetadata
        {
            Property = Gen.Random.Items(new[] { true, false })(),
            Nested = new NestedContent
            {
                Content = Gen.Random.Text.VeryLong()()
            }
        };

        public static SampleSnapshot GetSnapshot() => new SampleSnapshot
        {
            Number = Gen.Random.Numbers.Doubles().BetweenZeroAndOne()(),
            Nested = new NestedContent
            {
                Content = Gen.Random.Text.VeryLong()()
            }
        };
    }
}
