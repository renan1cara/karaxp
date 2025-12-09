using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Drawing;
using NAudio.Wave;

namespace KaraokePlayer
{
    public class MainForm : Form
    {
        private Panel panelLeft;
        private Panel panelRight;
        private ListBox listGenres;
        private ListBox listArtists;
        private ListBox listSongs;
        private Button btnPlay;
        private Button btnPause;
        private Button btnFullscreen;
        private Button btnImportUsb;
        private AxWMPLib.AxWindowsMediaPlayer axWindowsMediaPlayer1;

        private readonly string _libraryRoot;
        private readonly List<LibraryItem> _allSongs = new List<LibraryItem>();

        private bool _isFullscreen = false;
        private FormBorderStyle _prevBorderStyle;
        private bool _prevControlBox;
        private bool _prevTopMost;
        private Rectangle _prevBounds;

        private bool _isScoring = false;
        private int _scoreTotalTicks = 0;
        private int _scoreActiveTicks = 0;
        private float _currentLevel = 0f;

        private WaveInEvent _waveIn;
        private Timer _scoreTimer;

        public MainForm()
        {
            InitializeComponent();

            _libraryRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "library");
            if (!Directory.Exists(_libraryRoot))
                Directory.CreateDirectory(_libraryRoot);

            _scoreTimer = new Timer();
            _scoreTimer.Interval = 100;
            _scoreTimer.Tick += ScoreTimer_Tick;

            LoadLibrary();
        }

        private void InitializeComponent()
        {
            this.panelLeft = new Panel();
            this.panelRight = new Panel();
            this.listGenres = new ListBox();
            this.listArtists = new ListBox();
            this.listSongs = new ListBox();
            this.btnPlay = new Button();
            this.btnPause = new Button();
            this.btnFullscreen = new Button();
            this.btnImportUsb = new Button();
            this.axWindowsMediaPlayer1 = new AxWMPLib.AxWindowsMediaPlayer();

            ((System.ComponentModel.ISupportInitialize)(this.axWindowsMediaPlayer1)).BeginInit();
            this.SuspendLayout();

            this.Text = "Karaokê Player";
            this.ClientSize = new Size(1000, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.KeyPreview = true;

            this.panelLeft.Dock = DockStyle.Left;
            this.panelLeft.Width = 260;
            this.panelLeft.Padding = new Padding(5);

            this.listGenres.Dock = DockStyle.Top;
            this.listGenres.Height = 100;
            this.listGenres.SelectedIndexChanged += listGenres_SelectedIndexChanged;

            this.listArtists.Dock = DockStyle.Top;
            this.listArtists.Height = 100;
            this.listArtists.SelectedIndexChanged += listArtists_SelectedIndexChanged;

            this.listSongs.Dock = DockStyle.Fill;
            this.listSongs.DoubleClick += listSongs_DoubleClick;

            this.panelLeft.Controls.Add(this.listSongs);
            this.panelLeft.Controls.Add(this.listArtists);
            this.panelLeft.Controls.Add(this.listGenres);

            this.panelRight.Dock = DockStyle.Fill;
            this.panelRight.Padding = new Padding(5);

            this.btnPlay.Text = "Play (F1)";
            this.btnPlay.Left = 10;
            this.btnPlay.Top = 10;
            this.btnPlay.Width = 80;
            this.btnPlay.Click += btnPlay_Click;

            this.btnPause.Text = "Pause (F2)";
            this.btnPause.Left = 100;
            this.btnPause.Top = 10;
            this.btnPause.Width = 90;
            this.btnPause.Click += btnPause_Click;

            this.btnFullscreen.Text = "Tela Cheia (F3)";
            this.btnFullscreen.Left = 200;
            this.btnFullscreen.Top = 10;
            this.btnFullscreen.Width = 110;
            this.btnFullscreen.Click += btnFullscreen_Click;

            this.btnImportUsb.Text = "Importar Pendrive";
            this.btnImportUsb.Left = 320;
            this.btnImportUsb.Top = 10;
            this.btnImportUsb.Width = 140;
            this.btnImportUsb.Click += btnImportUsb_Click;

            this.axWindowsMediaPlayer1.Enabled = true;
            this.axWindowsMediaPlayer1.Left = 10;
            this.axWindowsMediaPlayer1.Top = 50;
            this.axWindowsMediaPlayer1.Width = this.panelRight.ClientSize.Width - 20;
            this.axWindowsMediaPlayer1.Height = this.panelRight.ClientSize.Height - 60;
            this.axWindowsMediaPlayer1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            this.axWindowsMediaPlayer1.PlayStateChange += axWindowsMediaPlayer1_PlayStateChange;

            this.panelRight.Controls.Add(this.axWindowsMediaPlayer1);
            this.panelRight.Controls.Add(this.btnImportUsb);
            this.panelRight.Controls.Add(this.btnFullscreen);
            this.panelRight.Controls.Add(this.btnPause);
            this.panelRight.Controls.Add(this.btnPlay);

            this.Controls.Add(this.panelRight);
            this.Controls.Add(this.panelLeft);

            ((System.ComponentModel.ISupportInitialize)(this.axWindowsMediaPlayer1)).EndInit();
            this.ResumeLayout(false);
        }

        private void LoadLibrary()
        {
            _allSongs.Clear();
            listGenres.Items.Clear();
            listArtists.Items.Clear();
            listSongs.Items.Clear();

            if (!Directory.Exists(_libraryRoot)) return;

            foreach (var genreDir in Directory.GetDirectories(_libraryRoot))
            {
                string genre = Path.GetFileName(genreDir);
                listGenres.Items.Add(genre);

                foreach (var artistDir in Directory.GetDirectories(genreDir))
                {
                    string artist = Path.GetFileName(artistDir);

                    foreach (var file in Directory.GetFiles(artistDir))
                    {
                        string ext = Path.GetExtension(file).ToLower();
                        if (ext == ".mp4" || ext == ".avi" || ext == ".wmv" || ext == ".mov" || ext == ".mkv")
                        {
                            _allSongs.Add(new LibraryItem
                            {
                                Genre = genre,
                                Artist = artist,
                                SongName = Path.GetFileNameWithoutExtension(file),
                                FilePath = file
                            });
                        }
                    }
                }
            }
        }

        private void RefreshArtists()
        {
            listArtists.Items.Clear();
            listSongs.Items.Clear();

            string genre = listGenres.SelectedItem as string;
            if (genre == null) return;

            HashSet<string> artists = new HashSet<string>();

            foreach (var item in _allSongs)
                if (item.Genre == genre)
                    artists.Add(item.Artist);

            foreach (var a in artists)
                listArtists.Items.Add(a);
        }

        private void RefreshSongs()
        {
            listSongs.Items.Clear();

            string genre = listGenres.SelectedItem as string;
            string artist = listArtists.SelectedItem as string;
            if (genre == null || artist == null) return;

            foreach (var item in _allSongs)
                if (item.Genre == genre && item.Artist == artist)
                    listSongs.Items.Add(item);
        }

        private void PlaySelected()
        {
            var item = listSongs.SelectedItem as LibraryItem;
            if (item == null) return;

            axWindowsMediaPlayer1.URL = item.FilePath;
            axWindowsMediaPlayer1.Ctlcontrols.play();
        }

        private void PausePlayback()
        {
            axWindowsMediaPlayer1.Ctlcontrols.pause();
            StopScoring();
        }

        private void StopPlayback()
        {
            axWindowsMediaPlayer1.Ctlcontrols.stop();
            StopScoring();
        }

        private void ToggleFullscreen()
        {
            if (!_isFullscreen)
            {
                _prevBorderStyle = this.FormBorderStyle;
                _prevControlBox = this.ControlBox;
                _prevTopMost = this.TopMost;
                _prevBounds = this.Bounds;

                this.FormBorderStyle = FormBorderStyle.None;
                this.ControlBox = false;
                this.TopMost = true;
                this.WindowState = FormWindowState.Maximized;

                axWindowsMediaPlayer1.Dock = DockStyle.Fill;

                _isFullscreen = true;
            }
            else
            {
                this.FormBorderStyle = _prevBorderStyle;
                this.ControlBox = _prevControlBox;
                this.TopMost = _prevTopMost;
                this.Bounds = _prevBounds;
                this.WindowState = FormWindowState.Normal;

                axWindowsMediaPlayer1.Dock = DockStyle.None;
                axWindowsMediaPlayer1.Left = 10;
                axWindowsMediaPlayer1.Top = 50;

                _isFullscreen = false;
            }
        }

        private void ImportFromUsb(string driveLetter)
        {
            string root = driveLetter + @":\";
            if (!Directory.Exists(root))
            {
                MessageBox.Show("Drive não encontrado.");
                return;
            }

            int imported = 0;

            foreach (var dir in Directory.GetDirectories(root))
            {
                string name = Path.GetFileName(dir);
                if (name.Length != 1) continue;

                foreach (var file in Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories))
                {
                    string rel = file.Substring(root.Length);
                    string dest = Path.Combine(_libraryRoot, rel);
                    Directory.CreateDirectory(Path.GetDirectoryName(dest));
                    File.Copy(file, dest, true);
                    imported++;
                }
            }

            LoadLibrary();
            MessageBox.Show($"Importados: {imported}");
        }

        private void StartMic()
        {
            try
            {
                if (_waveIn != null)
                {
                    _waveIn.StopRecording();
                    _waveIn.Dispose();
                }

                _waveIn = new WaveInEvent();
                _waveIn.DeviceNumber = 0;
                _waveIn.WaveFormat = new WaveFormat(44100, 1);
                _waveIn.DataAvailable += WaveIn_DataAvailable;
                _waveIn.StartRecording();
            }
            catch { }
        }

        private void StopMic()
        {
            try
            {
                if (_waveIn != null)
                {
                    _waveIn.StopRecording();
                    _waveIn.Dispose();
                }
            }
            catch { }
        }

        private void WaveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            double sum = 0;
            int samples = e.BytesRecorded / 2;
            if (samples == 0) return;

            for (int i = 0; i < e.BytesRecorded; i += 2)
            {
                short sample = (short)((e.Buffer[i + 1] << 8) | e.Buffer[i]);
                double value = sample / 32768.0;
                sum += value * value;
            }

            _currentLevel = (float)Math.Sqrt(sum / samples);
        }

        private void StartScoring()
        {
            _scoreTotalTicks = 0;
            _scoreActiveTicks = 0;
            _isScoring = true;
            StartMic();
            _scoreTimer.Start();
        }

        private void StopScoring()
        {
            if (!_isScoring) return;

            _isScoring = false;
            _scoreTimer.Stop();
            StopMic();
            ShowScore();
        }

        private void ScoreTimer_Tick(object sender, EventArgs e)
        {
            if (!_isScoring) return;
            _scoreTotalTicks++;

            if (_currentLevel > 0.01f)
                _scoreActiveTicks++;
        }

        private void ShowScore()
        {
            double ratio = (_scoreTotalTicks == 0) ? 0 : (double)_scoreActiveTicks / _scoreTotalTicks;
            int percent = (int)(ratio * 100);

            string rating =
                ratio >= 0.8 ? "OURO" :
                ratio >= 0.5 ? "PRATA" :
                ratio >= 0.2 ? "BRONZE" :
                "PRECISA CANTAR MAIS ALTO";

            MessageBox.Show($"Participação: {percent}%\nNota: {rating}");
        }

        private void axWindowsMediaPlayer1_PlayStateChange(object sender, AxWMPLib._WMPOCXEvents_PlayStateChangeEvent e)
        {
            if (e.newState == 3)
                StartScoring();
            else if (e.newState == 8 || e.newState == 1)
                StopScoring();
        }

        private void listGenres_SelectedIndexChanged(object sender, EventArgs e) => RefreshArtists();
        private void listArtists_SelectedIndexChanged(object sender, EventArgs e) => RefreshSongs();
        private void listSongs_DoubleClick(object sender, EventArgs e) => PlaySelected();
        private void btnPlay_Click(object sender, EventArgs e) => PlaySelected();
        private void btnPause_Click(object sender, EventArgs e) => PausePlayback();
        private void btnFullscreen_Click(object sender, EventArgs e) => ToggleFullscreen();
        private void btnImportUsb_Click(object sender, EventArgs e) => ImportFromUsb("E");

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.F1) { PlaySelected(); return true; }
            if (keyData == Keys.F2) { PausePlayback(); return true; }
            if (keyData == Keys.F3) { ToggleFullscreen(); return true; }
            if (keyData == Keys.F4)
            {
                int i = listSongs.SelectedIndex;
                if (i >= 0 && i < listSongs.Items.Count - 1)
                {
                    listSongs.SelectedIndex = i + 1;
                    PlaySelected();
                }
                return true;
            }
            if (keyData == Keys.Escape && _isFullscreen)
            {
                PausePlayback(); ToggleFullscreen(); return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
