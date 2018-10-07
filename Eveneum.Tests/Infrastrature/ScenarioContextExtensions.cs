using TechTalk.SpecFlow;

namespace Eveneum.Tests.Infrastrature
{
    static class ScenarioContextExtensions
    {
        public static void SetStreamId(this ScenarioContext context, string streamId) => context.Set(streamId, nameof(GetStreamId));
        public static string GetStreamId(this ScenarioContext context) => TryGetValue<string>(context, nameof(GetStreamId));

        public static void SetHeaderMetadata(this ScenarioContext context, SampleMetadata metadata) => context.Set(metadata, nameof(GetHeaderMetadata));
        public static SampleMetadata GetHeaderMetadata(this ScenarioContext context) => TryGetValue<SampleMetadata>(context, nameof(GetHeaderMetadata));

        public static void SetNewEvents(this ScenarioContext context, SampleEvent[] events) => context.Set(events, nameof(GetNewEvents));
        public static SampleEvent[] GetNewEvents(this ScenarioContext context) => TryGetValue<SampleEvent[]>(context, nameof(GetNewEvents));

        private static T TryGetValue<T>(ScenarioContext context, string key)
        {
            T value;
            context.TryGetValue<T>(key, out value);

            return value;
        }
    }
}
