using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using MaterialDesignThemes.Wpf;
using TagLib;

namespace Music_App
{
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer timer;
        private bool isPlaying = false;
        private string[] musicFiles;
        private int currentTrackIndex = -1;
        private bool isScrubbing = false;

        public MainWindow()
        {
            InitializeComponent();
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
        }

        // Otevření souboru s hudbou
        private void btnFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "MP3 Files (*.mp3)|*.mp3";
            openFileDialog.Multiselect = true;
            if (openFileDialog.ShowDialog() == true)
            {
                musicFiles = openFileDialog.FileNames;
                currentTrackIndex = 0;
                TrackList.ItemsSource = musicFiles.Select(f => Path.GetFileNameWithoutExtension(f));
                PlayMusicFile(musicFiles[currentTrackIndex]);
                UpdateNextSongLabel();
            }
        }

        // Přehrání hudebního souboru
        private void PlayMusicFile(string filePath)
        {
            mediaElement.Source = new Uri(filePath);
            mediaElement.Play();
            isPlaying = true;
            btnPlay.ToolTip = "Pause";
            UpdatePlayButtonIcon(PackIconKind.Pause);
            UpdateAlbumCover(filePath);
            lblSongname.Text = Path.GetFileNameWithoutExtension(filePath);
            timer.Start();
        }

        // Aktualizace obalu alba
        private void UpdateAlbumCover(string filePath)
        {
            try
            {
                var file = TagLib.File.Create(filePath);
                var pictures = file.Tag.Pictures;
                if (pictures.Length > 0)
                {
                    var pic = pictures[0];
                    using (var ms = new MemoryStream(pic.Data.Data))
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.StreamSource = ms;
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        AlbumCover.Source = bitmap;
                    }
                }
                else
                {
                    AlbumCover.Source = new BitmapImage(new Uri("Images/DefaultAlbumCover.png", UriKind.Relative));
                }
            }
            catch
            {
                AlbumCover.Source = new BitmapImage(new Uri("Images/DefaultAlbumCover.png", UriKind.Relative));
            }
        }

        // Přepínání mezi přehráváním a pauzou
        private void btnPlay_Click(object sender, RoutedEventArgs e)
        {
            if (isPlaying)
            {
                mediaElement.Pause();
                isPlaying = false;
                btnPlay.ToolTip = "Play";
                UpdatePlayButtonIcon(PackIconKind.Play);
                timer.Stop();
            }
            else
            {
                mediaElement.Play();
                isPlaying = true;
                btnPlay.ToolTip = "Pause";
                UpdatePlayButtonIcon(PackIconKind.Pause);
                timer.Start();
            }
        }

        // Aktualizace ikony přehrávání
        private void UpdatePlayButtonIcon(PackIconKind iconKind)
        {
            btnPlay.Content = new PackIcon { Kind = iconKind, Width = 20, Height = 20 };
        }

        // Zavření aplikace
        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // Aktualizace časovače přehrávání
        private void Timer_Tick(object sender, EventArgs e)
        {
            if (!isScrubbing && mediaElement.NaturalDuration.HasTimeSpan)
            {
                lblCurrenttime.Text = mediaElement.Position.ToString(@"mm\:ss");
                TimerSlider.Value = mediaElement.Position.TotalSeconds;
            }
        }

        // Spuštění přehrávání média
        private void mediaElement_MediaOpened(object sender, RoutedEventArgs e)
        {
            if (mediaElement.NaturalDuration.HasTimeSpan)
            {
                lblMusiclength.Text = mediaElement.NaturalDuration.TimeSpan.ToString(@"mm\:ss");
                TimerSlider.Maximum = mediaElement.NaturalDuration.TimeSpan.TotalSeconds;
                TimerSlider.Value = 0;
                lblSongname.Text = Path.GetFileNameWithoutExtension(musicFiles[currentTrackIndex]);
            }
        }

        // Změna hodnoty posuvníku časovače
        private void TimerSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (isScrubbing)
            {
                if (mediaElement.NaturalDuration.HasTimeSpan)
                {
                    double position = TimerSlider.Value;
                    mediaElement.Position = TimeSpan.FromSeconds(position);
                }
            }
        }

        // Zachycení počáteční události posouvání
        private void TimerSlider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            isScrubbing = true;
        }

        // Zachycení ukončení události posouvání
        private void TimerSlider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            isScrubbing = false;
            if (mediaElement.NaturalDuration.HasTimeSpan)
            {
                double position = TimerSlider.Value;
                mediaElement.Position = TimeSpan.FromSeconds(position);
            }
        }

        // Přehrání další skladby po skončení aktuální
        private void mediaElement_MediaEnded(object sender, RoutedEventArgs e)
        {
            if (musicFiles != null && musicFiles.Length > 0)
            {
                currentTrackIndex = (currentTrackIndex + 1) % musicFiles.Length;
                PlayMusicFile(musicFiles[currentTrackIndex]);
                TrackList.SelectedIndex = currentTrackIndex;
                UpdateNextSongLabel();
            }
        }

        // Přehrání další skladby na tlačítku
        private void btnPNext_Click(object sender, RoutedEventArgs e)
        {
            if (musicFiles != null && musicFiles.Length > 0)
            {
                currentTrackIndex = (currentTrackIndex + 1) % musicFiles.Length;
                PlayMusicFile(musicFiles[currentTrackIndex]);
                TrackList.SelectedIndex = currentTrackIndex;
                UpdateNextSongLabel();
            }
        }

        // Přehrání předchozí skladby na tlačítku
        private void btnPRewind_Click(object sender, RoutedEventArgs e)
        {
            if (musicFiles != null && musicFiles.Length > 0)
            {
                currentTrackIndex = (currentTrackIndex - 1 + musicFiles.Length) % musicFiles.Length;
                PlayMusicFile(musicFiles[currentTrackIndex]);
                TrackList.SelectedIndex = currentTrackIndex;
                UpdateNextSongLabel();
            }
        }

        // Výběr skladby ze seznamu
        private void TrackList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TrackList.SelectedIndex != -1 && TrackList.SelectedIndex != currentTrackIndex)
            {
                currentTrackIndex = TrackList.SelectedIndex;
                PlayMusicFile(musicFiles[currentTrackIndex]);
                UpdateNextSongLabel();
            }
        }

        // Aktualizace názvu další skladby
        private void UpdateNextSongLabel()
        {
            if (musicFiles != null && musicFiles.Length > 1)
            {
                int nextTrackIndex = (currentTrackIndex + 1) % musicFiles.Length;
                lblNextSong.Text = $"Next Song: {Path.GetFileNameWithoutExtension(musicFiles[nextTrackIndex])}";
            }
            else
            {
                lblNextSong.Text = "Next Song: None";
            }
        }

        // Možnost tažení okna myší
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }
    }
}
