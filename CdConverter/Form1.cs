using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CdConverter
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            trackTable.AutoGenerateColumns = false;
        }

        private List<AudioTrack> tracks = new List<AudioTrack>();

        private const string ALLOWED_CHARACTERS = "qwertyuiopasdfghjklzxcvbnmQWERTYUIOPASDFGHJKLZXCVBNM1234567890 !@#$%^&*()-_=+[]{}\\;:<,>.?/~'";

        private void Form1_Load(object sender, EventArgs e)
        {
            
        }

        private void trackTable_DragDrop(object sender, DragEventArgs e)
        {
            //Get filenames of items dropped
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            
            //Add each
            foreach (var f in files)
            {
                //Get fileinfo
                FileInfo info = new FileInfo(f);

                //Attempt to query info
                string title = info.Name;
                string artist = "";
                if (FfmpegUtil.QueryFileMetadata(f, out Dictionary<string, string> metadata))
                {
                    if (metadata.TryGetValue("title", out string ttitle))
                        title = ttitle;
                    if (metadata.TryGetValue("artist", out string tartist))
                        artist = tartist;
                }

                //Add
                tracks.Add(new AudioTrack
                {
                    Number = tracks.Count + 1,
                    Title = title,
                    Artist = artist,
                    FileName = info
                });
            }

            //Refresh table
            trackTable.DataSource = tracks.ToArray();
        }

        private void trackTable_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
        }

        private static bool CheckSpecialCharacters(string text)
        {
            char[] chars = text.ToCharArray();
            foreach (var c in chars)
            {
                if (!ALLOWED_CHARACTERS.Contains(c))
                    return true;
            }
            return false;
        }

        private void btnConvert_Click(object sender, EventArgs e)
        {
            //Do some verification
            if (boxCdTitle.Text.Length == 0 || boxCdArist.Text.Length == 0)
            {
                MessageBox.Show("The CD title or artist cannot be blank.", "Validation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (CheckSpecialCharacters(boxCdTitle.Text))
            {
                MessageBox.Show("The CD title contains invalid special characters.", "Validation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (CheckSpecialCharacters(boxCdArist.Text))
            {
                MessageBox.Show("The CD artist contains invalid special characters.", "Validation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (tracks.Count == 0)
            {
                MessageBox.Show("No tracks are present. Add tracks to the disc before converting.", "Validation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (tracks.Count > 99)
            {
                MessageBox.Show("Spec forbids more than 99 tracks.", "Validation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            foreach (var track in tracks)
            {
                if (track.Artist.Length == 0 || track.Title.Length == 0)
                {
                    MessageBox.Show($"The title or artist of track {track.Number} cannot be blank.", "Validation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (CheckSpecialCharacters(track.Artist))
                {
                    MessageBox.Show($"The artist of track #{track.Number} contains invalid special characters.", "Validation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (CheckSpecialCharacters(track.Title))
                {
                    MessageBox.Show($"The title of track #{track.Number} contains invalid special characters.", "Validation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            //Prompt for output directory
            FolderBrowserDialog fd = new FolderBrowserDialog();
            fd.Description = "Choose output folder to save converted files.";
            fd.ShowNewFolderButton = true;
            if (fd.ShowDialog() != DialogResult.OK)
                return;

            //Create worker dialog and start
            WorkerDialog worker = new WorkerDialog(new DirectoryInfo(fd.SelectedPath), tracks.ToArray(), boxCdTitle.Text, boxCdArist.Text);
            worker.ShowDialog();
            MessageBox.Show(worker.DialogMessage, "Convert Done", MessageBoxButtons.OK, worker.DialogResult == DialogResult.OK ? MessageBoxIcon.Information : MessageBoxIcon.Error);
        }
    }
}
