using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VCS.Models;

namespace VCS.Utils
{
    public static class TimestampUtils
    {
        public static float TimestampToSeconds(string timestamp)
        {
            var parsed = new Regex("^(?:(?:(?<Hours>\\d+):)?(?<Minutes>\\d+):)?(?<Seconds>\\d+(?:\\.\\d+)?)$").Match(timestamp);
            if (parsed == null || !parsed.Success)
            {
                throw new FormatException();
            }
            var hours = parsed.Groups["Hours"].Success ? int.Parse(parsed.Groups["Hours"].Value) : 0;
            var minutes = parsed.Groups["Minutes"].Success ? int.Parse(parsed.Groups["Minutes"].Value) : 0;
            var seconds = parsed.Groups["Seconds"].Success ? float.Parse(parsed.Groups["Seconds"].Value, CultureInfo.InvariantCulture) : 0;
            var result = +seconds + minutes * 60 + hours * 3600;
            return result;
        }

        public static string SecondsToTimestamp(float seconds, bool noMillis = false, bool alwaysHours = false, bool alwaysMinutes = false)
        {
            var hours = seconds >= 3600 || alwaysHours ? Math.Truncate((noMillis ? Math.Round(seconds) : seconds) / 3600).ToString().PadLeft(2, '0') + ':' : "";
            var minutes = seconds >= 60 || alwaysHours || alwaysMinutes ? Math.Truncate((noMillis ? Math.Round(seconds) : seconds) % 3600 / 60).ToString().PadLeft(2, '0') + ':' : "";
            var secondsString = String.Format(CultureInfo.InvariantCulture, noMillis ? (seconds >= 60 || alwaysHours ? "{0:00}" : "{0:0}") : (seconds >= 60 || alwaysMinutes || alwaysHours ? "{0:00.000}" : "{0:0.000}"), ((noMillis ? Math.Round(seconds) : seconds) % 60));
            return $"{hours}{minutes}{secondsString}";
        }

        public static TimeSpan[] GetThumbnailsTimestamps(FileConfig config)
        {
            if (config.MediaInfo == null)
            {
                throw new Exception("FileConfig is not loaded");
            }
            var thumbnailsCount = config.Columns * config.Rows;
            var videoDuration = (float)config.MediaInfo.Duration.TotalSeconds;

            var thumbnailsTimeSpacing = videoDuration / (thumbnailsCount + 1);
            var thumbnailsTimestamps = new float[thumbnailsCount];
            for (var i = 0; i < thumbnailsCount; i++)
            {
                thumbnailsTimestamps[i] = (thumbnailsTimeSpacing * (i + 1));
            }

            var replacedTimestampIndexes = new List<int>();
            for (var i = 0; i < config.Highlights.Count; i++)
            {
                var closestIndex = getClosestIndex(thumbnailsTimestamps, replacedTimestampIndexes, config.Highlights[i].Time);
                if (closestIndex == -1)
                {
                    throw new Exception("There is more highlights than available thumbnails");
                }
                replacedTimestampIndexes.Add(closestIndex);
                thumbnailsTimestamps[closestIndex] = config.Highlights[i].Time;
            }

            return thumbnailsTimestamps.OrderBy(t => t).Select(t => TimeSpan.FromSeconds(t)).ToArray();
        }

        private static int getClosestIndex(float[] array, List<int> ignoreList, float target)
        {
            float? closestAbs = null;
            int closestIndex = -1;
            for (var i = 0; i < array.Length; i++)
            {
                if (ignoreList == null || ignoreList.IndexOf(i) != -1)
                {
                    continue;
                }
                var abs = Math.Abs(array[i] - target);
                if (closestAbs == null || abs < closestAbs)
                {
                    closestAbs = abs;
                    closestIndex = i;
                }
            }
            return closestIndex;
        }
    }
}
