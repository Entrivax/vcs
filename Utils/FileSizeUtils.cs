using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCS.Utils
{
    class FileSizeUtils
    {
        const long KB = 1000;
        const long MB = 1000000;
        const long GB = 1000000000;
        const long TB = 1000000000000;
        const long PB = 1000000000000000;
        public static string GetFileSizeString(long bytes)
        {
            double size = bytes;
            string suffix = "B";

            if (bytes >= PB)
            {
                size = Math.Round((double)bytes / PB, 2);
                suffix = "PB";
            }
            else if (bytes >= TB)
            {
                size = Math.Round((double)bytes / TB, 2);
                suffix = "TB";
            }
            else if (bytes >= GB)
            {
                size = Math.Round((double)bytes / GB, 2);
                suffix = "GB";
            }
            else if (bytes >= MB)
            {
                size = Math.Round((double)bytes / MB, 2);
                suffix = "MB";
            }
            else if (bytes >= KB)
            {
                size = Math.Round((double)bytes / KB, 2);
                suffix = "KB";
            }

            return $"{size.ToString(CultureInfo.InvariantCulture)} {suffix}";
        }
    }
}
