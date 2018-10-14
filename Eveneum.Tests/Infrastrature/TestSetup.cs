using System.Collections.Generic;
using System.Linq;
using RandomGen;

namespace Eveneum.Tests.Infrastrature
{
    static class TestSetup
    {
        public static EventData[] GetEvents(int count = 5, int startVersion = 1)
        {
            var numbers = Gen.Random.Numbers.Decimals();
            var strings = Gen.Random.Text.VeryLong();

            return Enumerable.Range(startVersion, count)
                .Select(x => new SampleEvent
                {
                    Version = x,
                    Number = numbers(),
                    Nested = new NestedContent
                    {
                        Content = strings()
                    }
                })
                .Select(x => new EventData((ulong)x.Version, x))
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
