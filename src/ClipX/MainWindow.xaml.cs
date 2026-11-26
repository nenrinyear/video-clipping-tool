using System.IO;
using System.Windows;
using System.Windows.Threading;
using LibVLCSharp.Shared;
using Microsoft.Win32;

namespace ClipX;

public partial class MainWindow : Window
{
    private readonly FfmpegService ffmpeg = new();
    private readonly LibVLC libVlc;
    private readonly MediaPlayer mediaPlayer;
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(100) };
    private Media? vlcMedia;
    private string? inputPath;
    private MediaInfo? media;
    private bool playing, playerReady;
    private double currentPosition;

    public MainWindow()
    {
        InitializeComponent();
        Core.Initialize();
        libVlc = new LibVLC("--no-video-title-show", "--file-caching=250", "--avcodec-hw=any");
        mediaPlayer = new MediaPlayer(libVlc) { EnableHardwareDecoding = true };
        VideoView.MediaPlayer = mediaPlayer;
        mediaPlayer.EndReached += (_, _) => Dispatcher.BeginInvoke(() =>
        {
            playing = false;
            PlayButton.Content = "▶";
        });
        mediaPlayer.EncounteredError += (_, _) => Dispatcher.BeginInvoke(() =>
            FileNameText.Text = "VLCで動画を再生できませんでした");
        timer.Tick += (_, _) => TickPlayer();
        timer.Start();
        Closed += (_, _) =>
        {
            mediaPlayer.Stop();
            vlcMedia?.Dispose();
            mediaPlayer.Dispose();
            libVlc.Dispose();
        };
    }

    private void Open_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "動画ファイル|*.mp4;*.mov;*.m4v;*.webm;*.mkv;*.avi|すべてのファイル|*.*" };
        if (dialog.ShowDialog() == true) _ = LoadVideoAsync(dialog.FileName);
    }
    private void Window_DragOver(object sender, DragEventArgs e) { e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None; e.Handled = true; }
    private void Window_Drop(object sender, DragEventArgs e) { if (e.Data.GetData(DataFormats.FileDrop) is string[] { Length: > 0 } files) _ = LoadVideoAsync(files[0]); }

    private async Task LoadVideoAsync(string path)
    {
        if (!File.Exists(path)) return;
        if (!ffmpeg.IsAvailable) { MessageBox.Show("ffmpeg と ffprobe が見つかりません。\n\nアプリの tools フォルダーに ffmpeg.exe / ffprobe.exe を置くか、PATHを通してください。", "ClipX", MessageBoxButton.OK, MessageBoxImage.Information); return; }
        try
        {
            playing = false; playerReady = false; currentPosition = 0; mediaPlayer.Stop();
            vlcMedia?.Dispose(); vlcMedia = null;
            FileNameText.Text = "動画を解析中…"; ExportButton.IsEnabled = false; SnapshotButton.IsEnabled = false;
            media = await ffmpeg.ProbeAsync(path); inputPath = path;
            DropHint.Visibility = Visibility.Collapsed;
            Timeline.Duration = media.DurationSeconds;
            Timeline.RangeStart = 0; Timeline.RangeEnd = media.DurationSeconds; Timeline.Position = 0;
            vlcMedia = new Media(libVlc, new Uri(path));
            mediaPlayer.Media = vlcMedia;
            // Wait for VLC's decoder instead of guessing how long opening takes.
            var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            EventHandler<EventArgs>? onPlaying = null;
            onPlaying = (_, _) => started.TrySetResult();
            mediaPlayer.Playing += onPlaying;
            mediaPlayer.Play();
            await Task.WhenAny(started.Task, Task.Delay(3000));
            mediaPlayer.Playing -= onPlaying;
            await Task.Delay(80); // allow the first video frame to reach the view
            mediaPlayer.SetPause(true);
            mediaPlayer.Time = 0;
            playerReady = true;
            FileNameText.Text = $"{Path.GetFileName(path)}  •  {media.Width}×{media.Height}  •  {media.SizeBytes / 1_000_000d:0.0} MB";
            UpdateRangeUi(); ExportButton.IsEnabled = true; SnapshotButton.IsEnabled = true;
        }
        catch (Exception ex) { MessageBox.Show("動画を読み込めませんでした。\n" + ex.Message, "ClipX", MessageBoxButton.OK, MessageBoxImage.Error); FileNameText.Text = "読み込みに失敗しました"; }
    }

    private void Play_Click(object sender, RoutedEventArgs e)
    {
        if (inputPath is null || media is null || !playerReady) return;
        if (playing)
        {
            currentPosition = Math.Max(0, mediaPlayer.Time / 1000d);
            mediaPlayer.SetPause(true); playing = false; PlayButton.Content = "▶";
            return;
        }
        if (currentPosition >= Timeline.RangeEnd || currentPosition < Timeline.RangeStart) currentPosition = Timeline.RangeStart;
        var resumeAt = (long)(currentPosition * 1000);
        if (mediaPlayer.State == VLCState.Paused)
        {
            // Play() reloads some local media and can reset Time to zero. An
            // explicit unpause preserves the seek position.
            mediaPlayer.Time = resumeAt;
            mediaPlayer.SetPause(false);
        }
        else
        {
            EventHandler<EventArgs>? restorePosition = null;
            restorePosition = (_, _) =>
            {
                mediaPlayer.Playing -= restorePosition;
                mediaPlayer.Time = resumeAt;
            };
            mediaPlayer.Playing += restorePosition;
            mediaPlayer.Play();
        }
        playing = true; PlayButton.Content = "Ⅱ";
    }
    private void TickPlayer()
    {
        if (inputPath is null || media is null || !playerReady) return;
        if (playing) currentPosition = Math.Max(0, mediaPlayer.Time / 1000d);
        if (playing && currentPosition >= Timeline.RangeEnd)
        {
            mediaPlayer.SetPause(true); playing = false; PlayButton.Content = "▶";
            currentPosition = Timeline.RangeStart;
            mediaPlayer.Time = (long)(currentPosition * 1000);
        }
        Timeline.Position = currentPosition;
        CurrentTime.Text = $"{Format(currentPosition, false)} / {Format(media.DurationSeconds, false)}";
    }
    private void Timeline_InteractionStarted(object sender, TimelineInteractionEventArgs e)
    {
        if (e.Handle != TimelineHandle.Position) return;
        mediaPlayer.SetPause(true); playing = false; PlayButton.Content = "▶";
    }
    private void Timeline_ValueChangedByUser(object sender, TimelineInteractionEventArgs e)
    {
        if (e.Handle == TimelineHandle.Position)
        {
            currentPosition = e.Value;
            mediaPlayer.Time = (long)(currentPosition * 1000);
            CurrentTime.Text = $"{Format(currentPosition, false)} / {Format(media?.DurationSeconds ?? 0, false)}";
        }
        else UpdateRangeUi();
    }
    private void Timeline_InteractionCompleted(object sender, TimelineInteractionEventArgs e)
    {
        if (e.Handle == TimelineHandle.Position) mediaPlayer.NextFrame();
        else UpdateRangeUi();
    }
    private void SetStart_Click(object sender, RoutedEventArgs e)
    {
        Timeline.RangeStart = Math.Min(currentPosition, Timeline.RangeEnd - .1); UpdateRangeUi();
    }
    private void SetEnd_Click(object sender, RoutedEventArgs e)
    {
        Timeline.RangeEnd = Math.Max(currentPosition, Timeline.RangeStart + .1); UpdateRangeUi();
    }
    private void TimeText_LostFocus(object sender, RoutedEventArgs e)
    {
        if (TryParseTime(StartText.Text, out var start)) Timeline.RangeStart = Math.Clamp(start, 0, Timeline.Duration);
        if (TryParseTime(EndText.Text, out var end)) Timeline.RangeEnd = Math.Clamp(end, 0, Timeline.Duration);
        UpdateRangeUi();
    }
    private void Mode_Changed(object sender, RoutedEventArgs e) { if (IsLoaded) UpdateRangeUi(); }
    private void UpdateRangeUi()
    {
        if (StartText is null) return; var duration = Math.Max(0, Timeline.RangeEnd - Timeline.RangeStart);
        StartText.Text = Format(Timeline.RangeStart, true); EndText.Text = Format(Timeline.RangeEnd, true); SelectionLength.Text = $"選択: {Format(duration, true)}";
        var plan = BitratePlanner.Create(duration, 5, media?.HasAudio ?? true);
        if (CompressMode.IsChecked == true) { EstimateText.Text = plan.VideoKbps > 0 ? $"概算 {plan.ExpectedMegabytes:0.0} MB  •  映像 {plan.VideoKbps} kbps" : "範囲を指定してください"; AdviceText.Text = plan.Advice; ExportButton.IsEnabled = inputPath is not null && plan.IsViable; }
        else { var ratio = media is null || media.DurationSeconds <= 0 ? 0 : duration / media.DurationSeconds; EstimateText.Text = media is null ? "動画を選択すると概算を表示します" : $"概算 {media.SizeBytes * ratio / 1_000_000d:0.0} MB（元動画から推定）"; AdviceText.Text = "容量は5MBを超える可能性があります。"; ExportButton.IsEnabled = inputPath is not null; }
    }

    private async void Export_Click(object sender, RoutedEventArgs e)
    {
        if (inputPath is null || media is null) return;
        var compress = CompressMode.IsChecked == true; var plan = BitratePlanner.Create(Timeline.RangeEnd - Timeline.RangeStart, 5, media.HasAudio);
        var save = new SaveFileDialog { Filter = "MP4動画|*.mp4", FileName = Path.GetFileNameWithoutExtension(inputPath) + "_clip.mp4", InitialDirectory = Path.GetDirectoryName(inputPath) };
        if (save.ShowDialog() != true) return;
        try
        {
            ExportButton.IsEnabled = false; ExportButton.Content = "処理中…"; Progress.Visibility = Visibility.Visible; Progress.Value = 0;
            var progress = new Progress<double>(v => Progress.Value = v);
            await ffmpeg.ExportAsync(inputPath, save.FileName, Timeline.RangeStart, Timeline.RangeEnd, compress, plan, progress, CancellationToken.None);
            var size = new FileInfo(save.FileName).Length / 1_000_000d;
            MessageBox.Show($"書き出しが完了しました。\n{Path.GetFileName(save.FileName)}\n{size:0.00} MB", "ClipX", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex) { MessageBox.Show("書き出しに失敗しました。\n" + ex.Message, "ClipX", MessageBoxButton.OK, MessageBoxImage.Error); }
        finally { ExportButton.Content = "書き出す"; Progress.Visibility = Visibility.Collapsed; UpdateRangeUi(); }
    }
    private async void Snapshot_Click(object sender, RoutedEventArgs e)
    {
        if (inputPath is null || media is null || !playerReady) return;
        // Capture the frame the player is actually showing, not the 10Hz-cached position.
        var at = Math.Clamp(mediaPlayer.Time / 1000d, 0, Math.Max(0, media.DurationSeconds - 0.001));
        var save = new SaveFileDialog { Filter = "PNG画像|*.png|JPEG画像|*.jpg;*.jpeg", FileName = $"{Path.GetFileNameWithoutExtension(inputPath)}_{Format(at, true).Replace(':', 'm')}.png", InitialDirectory = Path.GetDirectoryName(inputPath) };
        if (save.ShowDialog() != true) return;
        try
        {
            await ffmpeg.CaptureFrameAsync(inputPath, at, save.FileName, CancellationToken.None);
            var size = new FileInfo(save.FileName).Length / 1024d;
            MessageBox.Show($"{Format(at, true)} のフレームを保存しました。\n{Path.GetFileName(save.FileName)}\n{size:0.#} KB", "ClipX", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex) { MessageBox.Show("フレームを保存できませんでした。\n" + ex.Message, "ClipX", MessageBoxButton.OK, MessageBoxImage.Error); }
    }
    private static string Format(double seconds, bool millis) { var t = TimeSpan.FromSeconds(Math.Max(0, seconds)); return millis ? $"{(int)t.TotalMinutes:00}:{t.Seconds:00}.{t.Milliseconds:000}" : $"{(int)t.TotalMinutes:00}:{t.Seconds:00}"; }
    private static bool TryParseTime(string text, out double seconds) { seconds = 0; var p = text.Trim().Split(':'); if (p.Length != 2 || !double.TryParse(p[0], out var min) || !double.TryParse(p[1], out var sec)) return false; seconds = min * 60 + sec; return seconds >= 0; }
}
