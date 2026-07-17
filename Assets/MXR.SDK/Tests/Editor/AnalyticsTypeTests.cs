using System;
using System.Collections.Generic;

using Newtonsoft.Json;

using NUnit.Framework;

namespace MXR.SDK.Tests {
    public class AnalyticsTypeTests {

        static AnalyticsEventPayload CreateVideoPlaybackStoppedPayload() =>
            new AnalyticsEventPayload {
                name = "video_playback_stopped",
                properties = new Dictionary<string, object> {
                    ["videoId"] = "abc123",
                    ["videoTitle"] = "Test video",
                    ["sessionId"] = "550e8400-e29b-41d4-a716-446655440000",
                    ["segmentIndex"] = 0,
                    ["segmentEventId"] = "7c9e6679-7425-40de-944b-e07fc1f90ae7",
                    ["startTime"] = 1720000000000L,
                    ["endTime"] = 1720000120000L,
                    ["duration"] = 120000L,
                    ["videoDurationMs"] = 600000L,
                    ["endReason"] = "paused",
                    ["replayIndex"] = 0,
                }
            };

        [Test]
        public void Serialize_SerializesEventName() {
            var json = JsonConvert.SerializeObject(CreateVideoPlaybackStoppedPayload());
            var payload = JsonConvert.DeserializeObject<AnalyticsEventPayload>(json);

            Assert.AreEqual("video_playback_stopped", payload.name);
        }

        [Test]
        public void Serialize_SerializesVideoPlaybackStoppedProperties() {
            var json = JsonConvert.SerializeObject(CreateVideoPlaybackStoppedPayload());
            var payload = JsonConvert.DeserializeObject<AnalyticsEventPayload>(json);
            var properties = payload.properties;

            Assert.NotNull(properties);
            Assert.AreEqual("abc123", properties["videoId"]);
            Assert.AreEqual("Test video", properties["videoTitle"]);
            Assert.AreEqual("550e8400-e29b-41d4-a716-446655440000", properties["sessionId"]);
            Assert.AreEqual(0, Convert.ToInt32(properties["segmentIndex"]));
            Assert.AreEqual("7c9e6679-7425-40de-944b-e07fc1f90ae7", properties["segmentEventId"]);
            Assert.AreEqual(1720000000000L, Convert.ToInt64(properties["startTime"]));
            Assert.AreEqual(1720000120000L, Convert.ToInt64(properties["endTime"]));
            Assert.AreEqual(120000L, Convert.ToInt64(properties["duration"]));
            Assert.AreEqual(600000L, Convert.ToInt64(properties["videoDurationMs"]));
            Assert.AreEqual("paused", properties["endReason"]);
            Assert.AreEqual(0, Convert.ToInt32(properties["replayIndex"]));
        }

        [Test]
        public void Serialize_DurationEqualsEndTimeMinusStartTime() {
            var json = JsonConvert.SerializeObject(CreateVideoPlaybackStoppedPayload());
            var payload = JsonConvert.DeserializeObject<AnalyticsEventPayload>(json);
            var properties = payload.properties;

            var startTime = Convert.ToInt64(properties["startTime"]);
            var endTime = Convert.ToInt64(properties["endTime"]);
            var duration = Convert.ToInt64(properties["duration"]);

            Assert.AreEqual(endTime - startTime, duration);
        }

        [Test]
        public void ToJson_MatchesJsonConvertSerializeObject() {
            var payload = CreateVideoPlaybackStoppedPayload();

            Assert.AreEqual(JsonConvert.SerializeObject(payload), payload.ToJson());
        }

        [Test]
        public void Deserialize_RoundTrips_NameAndProperties() {
            var original = CreateVideoPlaybackStoppedPayload();
            var json = JsonConvert.SerializeObject(original);

            var roundTripped = JsonConvert.DeserializeObject<AnalyticsEventPayload>(json);

            Assert.AreEqual(original.name, roundTripped.name);
            Assert.AreEqual(original.properties.Count, roundTripped.properties.Count);
            Assert.AreEqual(original.properties["videoId"], roundTripped.properties["videoId"]);
            Assert.AreEqual(original.properties["endReason"], roundTripped.properties["endReason"]);
            Assert.AreEqual(Convert.ToInt64(original.properties["duration"]),
                Convert.ToInt64(roundTripped.properties["duration"]));
        }

        [Test]
        public void Deserialize_FromWireFormatJson() {
            const string json =
                "{\"name\":\"video_playback_stopped\",\"properties\":{\"videoId\":\"abc123\",\"videoTitle\":\"Test video\",\"sessionId\":\"550e8400-e29b-41d4-a716-446655440000\",\"segmentIndex\":0,\"segmentEventId\":\"7c9e6679-7425-40de-944b-e07fc1f90ae7\",\"startTime\":1720000000000,\"endTime\":1720000120000,\"duration\":120000,\"videoDurationMs\":600000,\"endReason\":\"paused\",\"replayIndex\":0}}";

            var payload = JsonConvert.DeserializeObject<AnalyticsEventPayload>(json);

            Assert.AreEqual("video_playback_stopped", payload.name);
            Assert.AreEqual("abc123", payload.properties["videoId"]);
            Assert.AreEqual("paused", payload.properties["endReason"]);
        }
    }
}
