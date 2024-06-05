using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Net.Mime.MediaTypeNames;

namespace CdConverter
{
    public partial class WorkerDialog : Form
    {
        public WorkerDialog(DirectoryInfo outputDir, AudioTrack[] tracks, string cdTitle, string cdArtist)
        {
            this.outputDir = outputDir;
            this.tracks = tracks;
            this.cdTitle = cdTitle;
            this.cdArtist = cdArtist;
            InitializeComponent();
        }

        private readonly DirectoryInfo outputDir;
        private readonly AudioTrack[] tracks;
        private readonly string cdTitle;
        private readonly string cdArtist;

        private Thread worker;

        public string DialogMessage { get; set; }

        private void UpdateStatus(int progress, string text)
        {
            Invoke((MethodInvoker)delegate
            {
                progressBar.Value = progress;
                statusText.Text = text;
            });
        }

        private void WorkerFinish(bool ok, string message)
        {
            Invoke((MethodInvoker)delegate
            {
                DialogMessage = message;
                DialogResult = ok ? DialogResult.OK : DialogResult.No;
                Close();
            });
        }

        private void WorkerDialog_Load(object sender, EventArgs e)
        {
            //Set up progress bar
            progressBar.Maximum = tracks.Length;

            //Spin up worker thread
            worker = new Thread(WorkerThread);
            worker.IsBackground = true;
            worker.Start();
        }

        private void WorkerThread()
        {
            //Process each track
            string[] outputFilenames = new string[tracks.Length];
            for (int i = 0; i < tracks.Length; i++)
            {
                //Create output filename
                outputFilenames[i] = outputDir.FullName + Path.DirectorySeparatorChar + $"track{i}_{tracks[i].FileName.Name}.wav";

                //Update status
                UpdateStatus(i, $"Converting track {i} of {tracks.Length}: {tracks[i].FileName}");

                //Convert
                if (!FfmpegUtil.ConvertFile(tracks[i].FileName.FullName, "-bitexact -map_metadata -1 -ac 2 -ar 44100 -map 0:a", outputFilenames[i]))
                {
                    WorkerFinish(false, $"Failed to convert track #{i + 1}: {tracks[i].FileName}");
                    return;
                }
            }

            //Update final time
            UpdateStatus(tracks.Length, "Writing CUE sheet...");

            //Write CUE sheet
            using (FileStream fs = new FileStream(outputDir.FullName + Path.DirectorySeparatorChar + "index.cue", FileMode.Create))
            using (StreamWriter sw = new StreamWriter(fs))
                WriteCueSheet(sw, outputFilenames);

            //Exit
            WorkerFinish(true, $"Successfully converted {tracks.Length} tracks.");
        }

        private void WriteCueSheet(StreamWriter sw, string[] outputFilenames)
        {
            //Write performer and artist
            sw.WriteLine($"PERFORMER \"{cdArtist}\"");
            sw.WriteLine($"TITLE \"{cdTitle}\"");

            //Write each track
            for (int i = 0; i < tracks.Length; i++)
            {
                sw.WriteLine($"FILE \"{outputFilenames[i]}\" WAVE");
                sw.WriteLine($"  TRACK {(i+1).ToString().PadLeft(2, '0')} AUDIO");
                sw.WriteLine($"    PERFORMER \"{tracks[i].Artist}\"");
                sw.WriteLine($"    TITLE \"{tracks[i].Title}\"");
                sw.WriteLine($"    PREGAP 00:02:00");
                sw.WriteLine($"    INDEX 01 00:00:00");
            }
        }
    }
}
