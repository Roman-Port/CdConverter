using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CdConverter
{
    public class AudioTrack
    {
        public int Number { get; set; }
        public string Title { get; set; }
        public string Artist { get; set; }
        public FileInfo FileName { get; set; }
        public string DisplayFileName => FileName.Name;
    }
}
