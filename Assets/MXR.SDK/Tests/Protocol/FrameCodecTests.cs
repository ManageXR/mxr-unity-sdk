using System;

using MXR.SDK.Protocol.Framing;

using NUnit.Framework;

namespace MXR.SDK.Protocol.Tests {
    public class FrameCodecTests {

        [Test]
        public void Encode_WritesBigEndianLengthPrefix() {
            var body = new byte[] { 0x01, 0x02, 0x03 };

            var frame = FrameCodec.Encode(body);

            Assert.AreEqual(new byte[] { 0x00, 0x00, 0x00, 0x03, 0x01, 0x02, 0x03 }, frame);
        }

        [Test]
        public void Encode_WritesBigEndianLengthPrefixForLargeBody() {
            var body = new byte[300];
            body[0] = 0xAA;
            body[299] = 0xBB;

            var frame = FrameCodec.Encode(body);

            Assert.AreEqual(0x00, frame[0]);
            Assert.AreEqual(0x00, frame[1]);
            Assert.AreEqual(0x01, frame[2]);
            Assert.AreEqual(0x2C, frame[3]);
            Assert.AreEqual(0xAA, frame[4]);
            Assert.AreEqual(0xBB, frame[frame.Length - 1]);
        }

        [Test]
        public void Encode_NullBody_Throws() {
            Assert.Throws<ArgumentNullException>(() => FrameCodec.Encode(null));
        }

        [Test]
        public void Encode_Decode_RoundTripsArbitraryBody() {
            var body = new byte[] { 0x00, 0xFF, 0x10, 0x7A, 0x00 };

            var frame = FrameCodec.Encode(body);
            var result = FrameCodec.TryDecode(frame, 0, frame.Length);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(FrameDecodeStatus.Decoded, result.Status);
            Assert.AreEqual(FrameDecodeError.None, result.Error);
            Assert.AreEqual(body, result.Body);
            Assert.AreEqual(frame.Length, result.BytesConsumed);
        }

        [Test]
        public void TryDecode_EmptyBuffer_ReturnsIncomplete() {
            var result = FrameCodec.TryDecode(Array.Empty<byte>(), 0, 0);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(FrameDecodeStatus.Incomplete, result.Status);
            Assert.AreEqual(FrameDecodeError.TruncatedFrame, result.Error);
            Assert.AreEqual(0, result.BytesConsumed);
        }

        [Test]
        public void TryDecode_PartialLengthPrefix_ReturnsIncomplete() {
            var buffer = new byte[] { 0x00, 0x00, 0x01 };

            var result = FrameCodec.TryDecode(buffer, 0, buffer.Length);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(FrameDecodeStatus.Incomplete, result.Status);
            Assert.AreEqual(FrameDecodeError.TruncatedFrame, result.Error);
            Assert.AreEqual(0, result.BytesConsumed);
        }

        [Test]
        public void TryDecode_ZeroLengthBody_IsAViolationThatConsumesNothing() {
            var buffer = new byte[] { 0x00, 0x00, 0x00, 0x00 };

            var result = FrameCodec.TryDecode(buffer, 0, buffer.Length);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(FrameDecodeStatus.Violation, result.Status);
            Assert.AreEqual(FrameDecodeError.ZeroLengthBody, result.Error);
            Assert.AreEqual(0, result.BytesConsumed);
            Assert.IsNull(result.Body);
        }

        [Test]
        public void TryDecode_OversizeDeclaredLength_IsAViolationThatConsumesNothing() {
            // 64 MiB + 1, with the declared body following as it would on the wire. Consuming
            // the prefix and reading on would treat body bytes as the next length prefix.
            var buffer = new byte[] { 0x04, 0x00, 0x00, 0x01, 0xDE, 0xAD, 0xBE, 0xEF };

            var result = FrameCodec.TryDecode(buffer, 0, buffer.Length);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(FrameDecodeStatus.Violation, result.Status);
            Assert.AreEqual(FrameDecodeError.OversizeFrame, result.Error);
            Assert.AreEqual(0, result.BytesConsumed);
            Assert.IsNull(result.Body);
        }

        [Test]
        public void TryDecode_PartialBody_ReturnsIncomplete() {
            var buffer = new byte[] { 0x00, 0x00, 0x00, 0x03, 0x01, 0x02 };

            var result = FrameCodec.TryDecode(buffer, 0, buffer.Length);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(FrameDecodeStatus.Incomplete, result.Status);
            Assert.AreEqual(FrameDecodeError.TruncatedFrame, result.Error);
            Assert.AreEqual(0, result.BytesConsumed);
        }

        [Test]
        public void Encode_AtMaxBodyLength_Succeeds() {
            var body = new byte[FrameCodec.MaxBodyLength];
            body[0] = 0xAB;
            body[body.Length - 1] = 0xCD;

            var frame = FrameCodec.Encode(body);
            var result = FrameCodec.TryDecode(frame, 0, frame.Length);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(FrameCodec.MaxBodyLength, result.Body.Length);
            Assert.AreEqual(0xAB, result.Body[0]);
            Assert.AreEqual(0xCD, result.Body[result.Body.Length - 1]);
        }

        [Test]
        public void TryDecode_AtMaxBodyLengthPrefix_WithPartialBody_ReturnsIncomplete() {
            var buffer = new byte[FrameCodec.LengthPrefixSize + 1];
            buffer[0] = 0x04;
            buffer[1] = 0x00;
            buffer[2] = 0x00;
            buffer[3] = 0x00;

            var result = FrameCodec.TryDecode(buffer, 0, buffer.Length);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(FrameDecodeStatus.Incomplete, result.Status);
            Assert.AreEqual(FrameDecodeError.TruncatedFrame, result.Error);
            Assert.AreEqual(0, result.BytesConsumed);
        }

        [Test]
        public void Encode_AboveMaxBodyLength_Throws() {
            var body = new byte[FrameCodec.MaxBodyLength + 1];

            var ex = Assert.Throws<ArgumentException>(() => FrameCodec.Encode(body));
            Assert.That(ex.Message, Does.Contain("64 MiB"));
        }

        [Test]
        public void Encode_EmptyBody_Throws() {
            Assert.Throws<ArgumentException>(() => FrameCodec.Encode(Array.Empty<byte>()));
        }

        [Test]
        public void TryDecode_DecodesFirstFrameFromBufferWithTrailingBytes() {
            var first = FrameCodec.Encode(new byte[] { 0x01 });
            var second = FrameCodec.Encode(new byte[] { 0x02, 0x03 });
            var buffer = new byte[first.Length + second.Length];
            Buffer.BlockCopy(first, 0, buffer, 0, first.Length);
            Buffer.BlockCopy(second, 0, buffer, first.Length, second.Length);

            var result = FrameCodec.TryDecode(buffer, 0, buffer.Length);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(new byte[] { 0x01 }, result.Body);
            Assert.AreEqual(first.Length, result.BytesConsumed);
        }

        [Test]
        public void TryDecode_NonZeroOffset_DecodesFrame() {
            var frame = FrameCodec.Encode(new byte[] { 0x0A, 0x0B });
            var junkPrefixLength = 5;
            var buffer = new byte[junkPrefixLength + frame.Length];
            buffer[0] = 0xFF;
            Buffer.BlockCopy(frame, 0, buffer, junkPrefixLength, frame.Length);

            var result = FrameCodec.TryDecode(buffer, junkPrefixLength, frame.Length);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(new byte[] { 0x0A, 0x0B }, result.Body);
            Assert.AreEqual(frame.Length, result.BytesConsumed);
        }

        [Test]
        public void TryDecode_DecodesTwoFramesSequentiallyFromSameBuffer() {
            var first = FrameCodec.Encode(new byte[] { 0x01 });
            var second = FrameCodec.Encode(new byte[] { 0x02, 0x03 });
            var buffer = new byte[first.Length + second.Length];
            Buffer.BlockCopy(first, 0, buffer, 0, first.Length);
            Buffer.BlockCopy(second, 0, buffer, first.Length, second.Length);

            var firstResult = FrameCodec.TryDecode(buffer, 0, buffer.Length);
            Assert.IsTrue(firstResult.Success);
            Assert.AreEqual(new byte[] { 0x01 }, firstResult.Body);

            var secondResult = FrameCodec.TryDecode(
                buffer,
                firstResult.BytesConsumed,
                buffer.Length - firstResult.BytesConsumed);

            Assert.IsTrue(secondResult.Success);
            Assert.AreEqual(new byte[] { 0x02, 0x03 }, secondResult.Body);
            Assert.AreEqual(second.Length, secondResult.BytesConsumed);
        }

        [Test]
        public void TryDecode_OffsetAtEndOfBufferWithZeroCount_ReturnsIncomplete() {
            var frame = FrameCodec.Encode(new byte[] { 0x01 });

            var result = FrameCodec.TryDecode(frame, frame.Length, 0);

            Assert.AreEqual(FrameDecodeStatus.Incomplete, result.Status);
        }

        [Test]
        public void TryDecode_NullBuffer_Throws() {
            Assert.Throws<ArgumentNullException>(() => FrameCodec.TryDecode(null, 0, 0));
        }

        [Test]
        public void TryDecode_NegativeOffset_ThrowsNamingOffset() {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(
                () => FrameCodec.TryDecode(new byte[4], -1, 4));

            Assert.AreEqual("offset", ex.ParamName);
        }

        [Test]
        public void TryDecode_OffsetPastEndOfBuffer_ThrowsNamingOffset() {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(
                () => FrameCodec.TryDecode(new byte[4], 5, 0));

            Assert.AreEqual("offset", ex.ParamName);
        }

        [Test]
        public void TryDecode_NegativeCount_ThrowsNamingCount() {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(
                () => FrameCodec.TryDecode(new byte[4], 0, -1));

            Assert.AreEqual("count", ex.ParamName);
        }

        [Test]
        public void TryDecode_CountPastEndOfBuffer_ThrowsNamingCount() {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(
                () => FrameCodec.TryDecode(new byte[4], 2, 3));

            Assert.AreEqual("count", ex.ParamName);
        }

        [Test]
        public void TryDecode_OffsetAndCountThatOverflowWhenAdded_ThrowsNamingCount() {
            var buffer = new byte[FrameCodec.LengthPrefixSize + 4];

            var ex = Assert.Throws<ArgumentOutOfRangeException>(
                () => FrameCodec.TryDecode(buffer, FrameCodec.LengthPrefixSize, int.MaxValue));

            Assert.AreEqual("count", ex.ParamName);
        }
    }
}
