using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Automatic_Replay_Buffer.Models.Helpers
{
    public class Utilities
    {
        // cleans up window titles by removing problematic characters
        public static string NormalizeTitle(string title)
        {
            if (string.IsNullOrEmpty(title)) return title;

            var sb = new StringBuilder(title.Length);
            foreach (var ch in title)
            {
                var category = char.GetUnicodeCategory(ch);

                // common characters that cause issues, just in case
                if (category == System.Globalization.UnicodeCategory.Control ||
                    category == System.Globalization.UnicodeCategory.Format ||
                    category == System.Globalization.UnicodeCategory.Surrogate ||
                    category == System.Globalization.UnicodeCategory.PrivateUse ||
                    category == System.Globalization.UnicodeCategory.OtherNotAssigned)
                    continue;

                // trademark, registered, copyright
                if (ch == '\u2122' || ch == '\u00AE' || ch == '\u00A9')
                    continue;

                sb.Append(ch);
            }

            // replace multiple whitespaces with a single space and trim
            var cleaned = Regex.Replace(sb.ToString(), @"\s+", " ").Trim();
            return cleaned;
        }

        // gets the executable path of a process by its PID, with workaround for anti-cheat
        public static string? GetProcessExecutablePath(int pid)
        {
            const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;
            IntPtr h = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
            if (h == IntPtr.Zero) return null;
            try
            {
                int capacity = 1024;
                var sb = new StringBuilder(capacity);
                if (QueryFullProcessImageName(h, 0, sb, ref capacity))
                    return sb.ToString();
                return null;
            }
            finally
            {
                CloseHandle(h);
            }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool QueryFullProcessImageName(IntPtr hProcess, int dwFlags, StringBuilder lpExeName, ref int lpdwSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);
    }
}
