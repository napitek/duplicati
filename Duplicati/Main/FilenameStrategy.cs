#region Disclaimer / License
// Copyright (C) 2008, Kenneth Skovhede
// http://www.hexad.dk, opensource@hexad.dk
// 
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
// 
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
// 
#endregion
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Duplicati.Main
{
    /// <summary>
    /// This class is responsible for naming remote files,
    /// and parsing remote filenames
    /// </summary>
    internal class FilenameStrategy
    {
        private bool m_useShortFilenames;
        private string m_timeSeperator;

        private const long TICKS_PR_SECOND = 10000000;

        public FilenameStrategy(Dictionary<string, string> options)
        {
            //--short-filenames
            //--time-separator
            m_useShortFilenames = options.ContainsKey("short-filenames");
            if (options.ContainsKey("time-separator"))
                m_timeSeperator = options["time-separator"];
            else
                m_timeSeperator = ":";
        }

        public string GenerateFilename(string prefix, BackupEntry.EntryType type, bool full, DateTime time, int volume)
        {
            return GenerateFilename(prefix, type, full, time) + ".vol" + volume.ToString();
        }

        public string GenerateFilename(string prefix, BackupEntry.EntryType type, bool full, DateTime time)
        {
            string t;
            if (type == BackupEntry.EntryType.Manifest)
                t = m_useShortFilenames ? "M" : "manifest";
            else if (type == BackupEntry.EntryType.Content)
                t = m_useShortFilenames ? "C" : "content";
            else if (type == BackupEntry.EntryType.Signature)
                t = m_useShortFilenames ? "S" : "signature";
            else
                throw new Exception("Invalid entry type specified");

            if (!m_useShortFilenames)
            {
                string datetime = time.ToString().Replace(":", m_timeSeperator);
                return prefix + "-" + (full ? "full" : "inc") + "-" + t + "." + datetime;
            }
            else
            {
                return prefix + "-" + t + (full ? "F" : "I") + (time.Ticks / TICKS_PR_SECOND).ToString("X");
            }
        }

        public BackupEntry DecodeFilename(string prefix, Duplicati.Backend.FileEntry fe)
        {
            string shortregexp = @"(?<prefix>%filename%)\-(?<type>(C|S|M)(?<inc>(F|I))(?<time>([A-F]|[a-f]|[0-9])+)\.(?<extension>.+)";
            string regexp = @"(?<prefix>%filename%)\-(?<inc>(full|inc))\-(?<type>(content|signature|manifest))\.(?<time>\d{2}\-\d{2}\-\d{4}.\d{2}\%timesep%\d{2}\%timesep%\d{2})\.(?<extension>.+)";
            regexp = regexp.Replace("%filename%", prefix).Replace("%timesep%", m_timeSeperator);

            Match m = Regex.Match(fe.Name, regexp);
            if (!m.Success)
                m = Regex.Match(fe.Name, shortregexp);
            if (!m.Success)
                return null;

            BackupEntry.EntryType type;
            if (m.Groups["type"].Value == "manifest" || m.Groups["type"].Value == "M")
                type = BackupEntry.EntryType.Manifest;
            else if (m.Groups["type"].Value == "content" || m.Groups["type"].Value == "C")
                type = BackupEntry.EntryType.Content;
            else if (m.Groups["type"].Value == "signature" || m.Groups["type"].Value == "S")
                type = BackupEntry.EntryType.Signature;
            else
                return null;

            bool isFull = m.Groups["inc"].Value == "full" || m.Groups["inc"].Value == "F";
            DateTime time;
            if (m.Groups["inc"].Value == "F") //Short format
                time = new DateTime(long.Parse(m.Groups["time"].Value, System.Globalization.NumberStyles.HexNumber) * TICKS_PR_SECOND);
            else
                time = DateTime.Parse(m.Groups["time"].Value.Replace(m_timeSeperator, ":"));

            return new BackupEntry(fe, time, type, isFull);
        }

    }
}
