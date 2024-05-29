using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CdConverter
{
    static class FfmpegUtil
    {
        private const string METADATA_EXE = "ffmpegcd.exe";
        private const string METADATA_START = ";FFMETADATA1";

        public static bool QueryFileMetadata(string filename, out Dictionary<string, string> metadata)
        {
            //Create out
            metadata = new Dictionary<string, string>();

            //Invoke ffmpeg
            Process ffmpeg = Process.Start(new ProcessStartInfo
            {
                FileName = METADATA_EXE,
                Arguments = $"-y -loglevel error -i \"{filename}\" -f ffmetadata -",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true
            });

            //Read output til we find ";FFMETADATA1"
            string line = ffmpeg.StandardOutput.ReadLine();
            while (line != null && !line.StartsWith(METADATA_START))
                line = ffmpeg.StandardOutput.ReadLine();

            //Check if we've found the line
            if (!line.StartsWith(METADATA_START))
                return false;

            //Read every line out to the dict
            line = ffmpeg.StandardOutput.ReadLine();
            while (line != null)
            {
                //Extract key
                int keyLen = line.IndexOf('=');
                if (keyLen == -1)
                    return false; // Invalid line

                //Get key and value
                string key = line.Substring(0, keyLen).ToLower();
                string value = line.Substring(keyLen + 1);

                //Set if not already, otherwise discard
                if (!metadata.ContainsKey(key))
                    metadata.Add(key, value);

                //Read next line
                line = ffmpeg.StandardOutput.ReadLine();
            }

            //Finally, make sure it exited correctly
            ffmpeg.WaitForExit();
            return ffmpeg.ExitCode == 0;
        }

        public static bool ConvertFile(string inputFilename, string additionalArgs, string outputFilename)
        {
            Process ffmpeg = Process.Start(new ProcessStartInfo
            {
                FileName = METADATA_EXE,
                Arguments = $"-y -i \"{inputFilename}\" {additionalArgs} \"{outputFilename}\"",
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true
            });
            ffmpeg.WaitForExit();
            return ffmpeg.ExitCode == 0;
        }
    }
}
