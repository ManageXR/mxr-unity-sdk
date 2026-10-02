using System;
using System.Threading.Tasks;

using MXR.SDK.Protocol;
using MXR.SDK.Protocol.Diagnostics;
using MXR.SDK.Protocol.Handshake;
using MXR.SDK.Protocol.Subscriptions;

using NUnit.Framework;

namespace MXR.SDK.Protocol.Tests {
    public class ConnectionDiagnosticsTests {
        ConnectionDiagnostics _diagnostics;

        [SetUp]
        public void SetUp() {
            _diagnostics = new ConnectionDiagnostics();
        }

        [Test]
        public void Defaults_AreDisconnectedNotReadyAndCompatibilityUnset() {
            Assert.AreEqual(TransportState.Disconnected, _diagnostics.TransportState);
            Assert.IsFalse(_diagnostics.IsTransportUp);
            Assert.IsFalse(_diagnostics.IsDataReady);
            Assert.IsNull(_diagnostics.Compatibility);
            Assert.AreEqual(IncompatibilityReason.None, _diagnostics.IncompatibilityReason);
            Assert.IsNull(_diagnostics.Error);
            Assert.IsNull(_diagnostics.EffectiveProtocol);
            Assert.IsNull(_diagnostics.SelectedFormat);
            Assert.IsNull(_diagnostics.RemoteImpl);
            Assert.AreEqual(0, _diagnostics.GetAllDeliverySnapshots().Count);
        }

        [Test]
        public void ApplyHandshake_Compatible_CopiesHandshakeFieldsAndRemoteImpl() {
            var result = HandshakeResult.CreateCompatible(
                2,
                JsonEnvelopeCodec.FormatName,
                EnvelopeCodecRegistry.HelloCodec);
            var serverHello = ServerHello("mxr-admin-app/2.0.0");

            _diagnostics.ApplyHandshake(result, serverHello);

            Assert.AreEqual(Compatibility.Compatible, _diagnostics.Compatibility);
            Assert.AreEqual(IncompatibilityReason.None, _diagnostics.IncompatibilityReason);
            Assert.IsNull(_diagnostics.Error);
            Assert.AreEqual(2, _diagnostics.EffectiveProtocol);
            Assert.AreEqual(JsonEnvelopeCodec.FormatName, _diagnostics.SelectedFormat);
            Assert.AreEqual("mxr-admin-app/2.0.0", _diagnostics.RemoteImpl);
            Assert.IsFalse(_diagnostics.IsTransportUp);
            Assert.IsFalse(_diagnostics.IsDataReady);
        }

        [Test]
        public void ApplyHandshake_Incompatible_SetsReasonAndErrorAndLeavesProtocolFormatNull() {
            var result = HandshakeResult.CreateIncompatible(
                IncompatibilityReason.AdminAppTooOld,
                "Admin App speaks protocol 1; this SDK requires at least 2. Update the Admin App.");
            var serverHello = ServerHello("mxr-admin-app/1.0.0");

            _diagnostics.ApplyHandshake(result, serverHello);

            Assert.AreEqual(Compatibility.Incompatible, _diagnostics.Compatibility);
            Assert.AreEqual(IncompatibilityReason.AdminAppTooOld, _diagnostics.IncompatibilityReason);
            Assert.AreEqual(result.Error, _diagnostics.Error);
            Assert.IsNull(_diagnostics.EffectiveProtocol);
            Assert.IsNull(_diagnostics.SelectedFormat);
            Assert.AreEqual("mxr-admin-app/1.0.0", _diagnostics.RemoteImpl);
        }

        [Test]
        public void ApplyHandshake_IncompatibleAfterCompatible_ClearsProtocolAndFormat() {
            _diagnostics.ApplyHandshake(
                HandshakeResult.CreateCompatible(
                    2,
                    JsonEnvelopeCodec.FormatName,
                    EnvelopeCodecRegistry.HelloCodec),
                ServerHello("mxr-admin-app/2.0.0"));

            _diagnostics.ApplyHandshake(
                HandshakeResult.CreateIncompatible(
                    IncompatibilityReason.NoCommonFormat,
                    "No common payload format."),
                ServerHello("mxr-admin-app/2.0.0"));

            Assert.AreEqual(Compatibility.Incompatible, _diagnostics.Compatibility);
            Assert.AreEqual(IncompatibilityReason.NoCommonFormat, _diagnostics.IncompatibilityReason);
            Assert.IsNull(_diagnostics.EffectiveProtocol);
            Assert.IsNull(_diagnostics.SelectedFormat);
        }

        [Test]
        public void ApplyHandshake_NullResult_Throws() {
            Assert.Throws<ArgumentNullException>(
                () => _diagnostics.ApplyHandshake(null, ServerHello("aa")));
        }

        [Test]
        public void ApplyHandshake_NullServerHello_Throws() {
            Assert.Throws<ArgumentNullException>(
                () => _diagnostics.ApplyHandshake(
                    HandshakeResult.CreateIncompatible(
                        IncompatibilityReason.SdkTooOld,
                        "Update the SDK."),
                    null));
        }

        [Test]
        public void SetIncompatible_RecordsReasonAndClearsHandshakeFields() {
            _diagnostics.SetIncompatible(
                IncompatibilityReason.AdminAppTooOld,
                "Admin App versionCode 10 < 20.");

            Assert.AreEqual(Compatibility.Incompatible, _diagnostics.Compatibility);
            Assert.AreEqual(IncompatibilityReason.AdminAppTooOld, _diagnostics.IncompatibilityReason);
            Assert.AreEqual("Admin App versionCode 10 < 20.", _diagnostics.Error);
            Assert.IsNull(_diagnostics.EffectiveProtocol);
            Assert.IsNull(_diagnostics.SelectedFormat);
            Assert.IsNull(_diagnostics.RemoteImpl);
        }

        [Test]
        public void SetIncompatible_AcceptsSdkTooOld() {
            _diagnostics.SetIncompatible(IncompatibilityReason.SdkTooOld, "Update the SDK.");

            Assert.AreEqual(IncompatibilityReason.SdkTooOld, _diagnostics.IncompatibilityReason);
            Assert.AreEqual("Update the SDK.", _diagnostics.Error);
        }

        [Test]
        public void SetIncompatible_NoneReason_Throws() {
            Assert.Throws<ArgumentException>(
                () => _diagnostics.SetIncompatible(IncompatibilityReason.None, "nope"));
        }

        [Test]
        public void SetIncompatible_NullError_Throws() {
            Assert.Throws<ArgumentNullException>(
                () => _diagnostics.SetIncompatible(IncompatibilityReason.AdminAppTooOld, null));
        }

        [Test]
        public void ApplyHandshake_Compatible_ClearsErrorAfterIncompatible() {
            _diagnostics.SetIncompatible(IncompatibilityReason.AdminAppTooOld, "old");

            _diagnostics.ApplyHandshake(
                HandshakeResult.CreateCompatible(
                    2,
                    JsonEnvelopeCodec.FormatName,
                    EnvelopeCodecRegistry.HelloCodec),
                ServerHello("mxr-admin-app/2.0.0"));

            Assert.AreEqual(Compatibility.Compatible, _diagnostics.Compatibility);
            Assert.AreEqual(IncompatibilityReason.None, _diagnostics.IncompatibilityReason);
            Assert.IsNull(_diagnostics.Error);
        }

        [Test]
        public void TransportAndReadiness_AreIndependent() {
            _diagnostics.SetTransportState(TransportState.Connecting);
            Assert.AreEqual(TransportState.Connecting, _diagnostics.TransportState);
            Assert.IsFalse(_diagnostics.IsTransportUp);
            Assert.IsFalse(_diagnostics.IsDataReady);

            _diagnostics.SetTransportState(TransportState.Connected);
            Assert.IsTrue(_diagnostics.IsTransportUp);
            Assert.IsFalse(_diagnostics.IsDataReady);

            _diagnostics.SetDataReady(true);
            Assert.IsTrue(_diagnostics.IsTransportUp);
            Assert.IsTrue(_diagnostics.IsDataReady);

            _diagnostics.SetTransportState(TransportState.Disconnected);
            Assert.IsFalse(_diagnostics.IsTransportUp);
            Assert.IsTrue(_diagnostics.IsDataReady);

            _diagnostics.SetDataReady(false);
            Assert.IsFalse(_diagnostics.IsTransportUp);
            Assert.IsFalse(_diagnostics.IsDataReady);
        }

        [Test]
        public void RecordSent_UnknownChannel_CreatesCounterEntry() {
            _diagnostics.RecordSent(StateChannelNames.DeviceStatus);

            var counters = _diagnostics.GetDeliverySnapshot(StateChannelNames.DeviceStatus);
            Assert.AreEqual(1, counters.Sent);
            Assert.AreEqual(0, counters.Acked);
            Assert.AreEqual(1, _diagnostics.GetAllDeliverySnapshots().Count);
            Assert.IsTrue(_diagnostics.GetAllDeliverySnapshots().ContainsKey(StateChannelNames.DeviceStatus));
        }

        [Test]
        public void GetDeliverySnapshot_UnknownChannel_ReturnsZerosWithoutInserting() {
            var counters = _diagnostics.GetDeliverySnapshot("wifi");

            Assert.AreEqual(0, counters.Sent);
            Assert.AreEqual(0, counters.Acked);
            Assert.AreEqual(0, counters.Replied);
            Assert.AreEqual(0, counters.RejectedPermanent);
            Assert.AreEqual(0, counters.RejectedRetryable);
            Assert.IsFalse(_diagnostics.GetAllDeliverySnapshots().ContainsKey("wifi"));
            Assert.AreEqual(0, _diagnostics.GetAllDeliverySnapshots().Count);

            _diagnostics.RecordSent("wifi");

            Assert.AreEqual(1, _diagnostics.GetDeliverySnapshot("wifi").Sent);
            Assert.IsTrue(_diagnostics.GetAllDeliverySnapshots().ContainsKey("wifi"));
        }

        [Test]
        public void RecordMethods_IncrementPerChannelIndependently() {
            _diagnostics.RecordSent(StateChannelNames.DeviceStatus);
            _diagnostics.RecordSent(StateChannelNames.DeviceStatus);
            _diagnostics.RecordAcked(StateChannelNames.DeviceStatus);
            _diagnostics.RecordReplied(StateChannelNames.DeviceStatus);
            _diagnostics.RecordRejectedPermanent(StateChannelNames.RuntimeSettings);
            _diagnostics.RecordRejectedRetryable(StateChannelNames.DeviceData);
            _diagnostics.RecordRejectedRetryable(StateChannelNames.DeviceData);

            var status = _diagnostics.GetDeliverySnapshot(StateChannelNames.DeviceStatus);
            Assert.AreEqual(2, status.Sent);
            Assert.AreEqual(1, status.Acked);
            Assert.AreEqual(1, status.Replied);
            Assert.AreEqual(0, status.RejectedPermanent);
            Assert.AreEqual(0, status.RejectedRetryable);

            var settings = _diagnostics.GetDeliverySnapshot(StateChannelNames.RuntimeSettings);
            Assert.AreEqual(0, settings.Sent);
            Assert.AreEqual(1, settings.RejectedPermanent);

            var data = _diagnostics.GetDeliverySnapshot(StateChannelNames.DeviceData);
            Assert.AreEqual(2, data.RejectedRetryable);

            Assert.AreEqual(3, _diagnostics.GetAllDeliverySnapshots().Count);
        }

        [Test]
        public void GetAllDeliverySnapshots_ReturnsCopy() {
            _diagnostics.RecordSent(StateChannelNames.DeviceStatus);

            var first = _diagnostics.GetAllDeliverySnapshots();
            _diagnostics.RecordSent(StateChannelNames.DeviceStatus);
            var second = _diagnostics.GetAllDeliverySnapshots();

            Assert.AreEqual(1, first[StateChannelNames.DeviceStatus].Sent);
            Assert.AreEqual(2, second[StateChannelNames.DeviceStatus].Sent);
        }

        [Test]
        public void Reset_ClearsSessionStateAndCounters() {
            _diagnostics.SetTransportState(TransportState.Connected);
            _diagnostics.SetDataReady(true);
            _diagnostics.ApplyHandshake(
                HandshakeResult.CreateCompatible(
                    2,
                    JsonEnvelopeCodec.FormatName,
                    EnvelopeCodecRegistry.HelloCodec),
                ServerHello("mxr-admin-app/2.0.0"));
            _diagnostics.RecordSent(StateChannelNames.DeviceStatus);
            _diagnostics.RecordAcked(StateChannelNames.DeviceStatus);

            _diagnostics.Reset();

            Assert.AreEqual(TransportState.Disconnected, _diagnostics.TransportState);
            Assert.IsFalse(_diagnostics.IsTransportUp);
            Assert.IsFalse(_diagnostics.IsDataReady);
            Assert.IsNull(_diagnostics.Compatibility);
            Assert.AreEqual(IncompatibilityReason.None, _diagnostics.IncompatibilityReason);
            Assert.IsNull(_diagnostics.Error);
            Assert.IsNull(_diagnostics.EffectiveProtocol);
            Assert.IsNull(_diagnostics.SelectedFormat);
            Assert.IsNull(_diagnostics.RemoteImpl);
            Assert.AreEqual(0, _diagnostics.GetAllDeliverySnapshots().Count);
            Assert.AreEqual(0, _diagnostics.GetDeliverySnapshot(StateChannelNames.DeviceStatus).Sent);
            Assert.AreEqual(0, _diagnostics.GetAllDeliverySnapshots().Count);
        }

        [Test]
        public void RecordAndRead_AreThreadSafe() {
            var channels = new[] {
                StateChannelNames.DeviceStatus,
                StateChannelNames.RuntimeSettings,
                StateChannelNames.DeviceData
            };

            Parallel.For(0, 150, i => {
                var channel = channels[i % channels.Length];
                _diagnostics.RecordSent(channel);
                _diagnostics.RecordAcked(channel);
                _diagnostics.GetDeliverySnapshot(channel);
                _diagnostics.SetTransportState(TransportState.Connected);
                _diagnostics.SetDataReady(i % 2 == 0);
                _ = _diagnostics.IsTransportUp;
                _ = _diagnostics.IsDataReady;
                _ = _diagnostics.GetAllDeliverySnapshots();
            });

            var all = _diagnostics.GetAllDeliverySnapshots();
            Assert.AreEqual(3, all.Count);
            Assert.AreEqual(50, all[StateChannelNames.DeviceStatus].Sent);
            Assert.AreEqual(50, all[StateChannelNames.DeviceStatus].Acked);
            Assert.AreEqual(50, all[StateChannelNames.RuntimeSettings].Sent);
            Assert.AreEqual(50, all[StateChannelNames.DeviceData].Sent);
        }

        [Test]
        public void RecordSent_NullChannel_Throws() {
            Assert.Throws<ArgumentNullException>(() => _diagnostics.RecordSent(null));
        }

        [Test]
        public void RecordSent_EmptyChannel_Throws() {
            Assert.Throws<ArgumentException>(() => _diagnostics.RecordSent(string.Empty));
        }

        [Test]
        public void GetDeliverySnapshot_NullChannel_Throws() {
            Assert.Throws<ArgumentNullException>(() => _diagnostics.GetDeliverySnapshot(null));
        }

        [Test]
        public void GetDeliverySnapshot_EmptyChannel_Throws() {
            Assert.Throws<ArgumentException>(() => _diagnostics.GetDeliverySnapshot(string.Empty));
        }

        static HelloPayload ServerHello(string impl) {
            return new HelloPayload {
                Protocol = 2,
                MinProtocol = 2,
                Formats = new[] { JsonEnvelopeCodec.FormatName },
                Impl = impl
            };
        }
    }
}
