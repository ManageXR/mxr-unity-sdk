using System;

namespace MXR.SDK {
    public static class UnixTimeUtils {
        public static DateTime FromUnixTimeSeconds(long seconds) {
            return DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime;
        }

        public static long ToUnixTimeSeconds(DateTime utcDateTime) {
            return ((DateTimeOffset)utcDateTime).ToUnixTimeSeconds();
        }

        public static bool HasExpired(long unixTimestampSeconds) {
            return FromUnixTimeSeconds(unixTimestampSeconds) <= DateTime.UtcNow;
        }

        public static TimeSpan TimeUntilExpiration(long unixTimestampSeconds) {
            return FromUnixTimeSeconds(unixTimestampSeconds) - DateTime.UtcNow;
        }
    }
}
