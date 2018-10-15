using Eveneum.Documents;
using System.Collections.Generic;
using System.Linq;
using TechTalk.SpecFlow;

namespace Eveneum.Tests.Infrastrature
{
    static class ScenarioContextExtensions
    {
        public static void SetStreamId(this ScenarioContext context, string streamId) => context.Set(streamId, nameof(GetStreamId));
        public static string GetStreamId(this ScenarioContext context) => TryGetValue<string>(context, nameof(GetStreamId));

        public static void SetHeaderMetadata(this ScenarioContext context, SampleMetadata metadata) => context.Set(metadata, nameof(GetHeaderMetadata));
        public static SampleMetadata GetHeaderMetadata(this ScenarioContext context) => TryGetValue<SampleMetadata>(context, nameof(GetHeaderMetadata));

        public static void SetSnapshot(this ScenarioContext context, SampleSnapshot snapshot) => context.Set(snapshot, nameof(GetSnapshot));
        public static SampleSnapshot GetSnapshot(this ScenarioContext context) => TryGetValue<SampleSnapshot>(context, nameof(GetSnapshot));

        public static void SetSnapshotMetadata(this ScenarioContext context, SampleMetadata metadata) => context.Set(metadata, nameof(GetSnapshotMetadata));
        public static SampleMetadata GetSnapshotMetadata(this ScenarioContext context) => TryGetValue<SampleMetadata>(context, nameof(GetSnapshotMetadata));

        public static void SetNewEvents(this ScenarioContext context, EventData[] events) => context.Set(events, nameof(GetNewEvents));
        public static EventData[] GetNewEvents(this ScenarioContext context) => TryGetValue<EventData[]>(context, nameof(GetNewEvents));

        public static void SetExistingDocuments(this ScenarioContext context, IEnumerable<EveneumDocument> documents) => context.Set(documents.ToArray(), nameof(GetExistingDocuments));
        public static EveneumDocument[] GetExistingDocuments(this ScenarioContext context) => TryGetValue<EveneumDocument[]>(context, nameof(GetExistingDocuments));

        private static T TryGetValue<T>(ScenarioContext context, string key)
        {
            T value;
            context.TryGetValue<T>(key, out value);

            return value;
        }
    }
}
