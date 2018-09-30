using System.Collections.Generic;
using System.Linq;
using RandomGen;

namespace Eveneum.Tests.Infrastrature
{
    static class TestSetup
    {
        public static IReadOnlyCollection<SampleEvent> GetEvents(int count = 5)
        {
            var numbers = Gen.Random.Numbers.Decimals();
            var strings = Gen.Random.Text.VeryLong();

            return Enumerable.Range(1, count).Select(x => new SampleEvent
            {
                Version = x,
                Number = numbers(),
                Nested = new NestedContent
                {
                    Content = strings()
                }
            }).ToList();
        }

        public static SampleMetadata GetMetadata() => new SampleMetadata
        {
            Property = Gen.Random.Items(new[] { true, false })(),
            Nested = new NestedContent
            {
                Content = Gen.Random.Text.VeryLong()()
            }
        };
    }
}
