using System;

namespace KaraokePlayer
{
    public class LibraryItem
    {
        public string Genre { get; set; }
        public string Artist { get; set; }
        public string SongName { get; set; }
        public string FilePath { get; set; }

        public override string ToString() => SongName;
    }
}
