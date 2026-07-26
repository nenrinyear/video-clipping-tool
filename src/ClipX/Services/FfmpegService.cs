using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ClipX;

public sealed class FfmpegService
{
    public string FfmpegPath { get; }
    public string FfprobePath { get; }
    public bool IsAvailable => Resolve(FfmpegPath) && Resolve(FfprobePath);

    public FfmpegService()
    {
        var baseDir = AppContext.BaseDirectory;
        FfmpegPath = File.Exists(Path.Combine(baseDir, "tools", "ffmpeg.exe")) ? Path.Combine(baseDir, "tools", "ffmpeg.exe") : "ffmpeg";
        FfprobePath = File.Exists(Path.Combine(baseDir, "tools", "ffprobe.exe")) ? Path.Combine(baseDir, "tools", "ffprobe.exe") : "ffprobe";
    }

    private static bool Resolve(string exe)
    {
        try { using var p = Process.Start(new ProcessStartInfo(exe, "-version") { UseShellExecute = false, CreateNoWindow = true }); p?.WaitForExit(2500); return p?.ExitCode == 0; }
        catch { return false; }
    }

    public async Task<MediaInfo> ProbeAsync(string input)
    {
        var json = await CaptureAsync(FfprobePath, $"-v error -show_entries format=duration,size -show_entries stream=codec_type,width,height -of json {Q(input)}");
        using var doc = JsonDocument.Parse(json);
        var format = doc.RootElement.GetProperty("format");
        var duration = double.Parse(format.GetProperty("duration").GetString()!, CultureInfo.InvariantCulture);
        var size = long.Parse(format.GetProperty("size").GetString()!, CultureInfo.InvariantCulture);
        var width = 0; var height = 0; var audio = false;
        foreach (var stream in doc.RootElement.GetProperty("streams").EnumerateArray())
        {
            var type = stream.GetProperty("codec_type").GetString();
            if (type == "audio") audio = true;
            if (type == "video") { width = stream.TryGetProperty("width", out var w) ? w.GetInt32() : 0; height = stream.TryGetProperty("height", out var h) ? h.GetInt32() : 0; }
        }
        return new(duration, size, width, height, audio);
    }

    public async Task ExportAsync(string input, string output, double start, double end, bool compress, EncodePlan plan, IProgress<double> progress, CancellationToken token)
    {
        var duration = end - start;
        string args;
        if (compress)
        {
            var scale = plan.VideoKbps < 350 ? "-vf scale='min(640,iw)':-2 " : plan.VideoKbps < 900 ? "-vf scale='min(960,iw)':-2 " : "";
            var audio = plan.AudioKbps > 0 ? $"-c:a aac -b:a {plan.AudioKbps}k" : "-an";
            args = $"-y -ss {F(start)} -i {Q(input)} -t {F(duration)} {scale}-c:v libx264 -preset medium -b:v {plan.VideoKbps}k -maxrate {plan.VideoKbps}k -bufsize {plan.VideoKbps * 2}k -pix_fmt yuv420p -movflags +faststart {audio} {Q(output)}";
        }
        else args = $"-y -ss {F(start)} -i {Q(input)} -t {F(duration)} -map 0:v:0 -map 0:a? -c copy -avoid_negative_ts make_zero {Q(output)}";

        var psi = new ProcessStartInfo(FfmpegPath, args) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true };
        using var process = Process.Start(psi) ?? throw new InvalidOperationException("ffmpegを起動できませんでした。");
        using var registration = token.Register(() => { try { process.Kill(true); } catch { } });
        while (await process.StandardError.ReadLineAsync(token) is { } line)
        {
            var m = Regex.Match(line, @"time=(\d+):(\d+):(\d+(?:\.\d+)?)");
            if (m.Success) { var seconds = int.Parse(m.Groups[1].Value) * 3600 + int.Parse(m.Groups[2].Value) * 60 + double.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture); progress.Report(Math.Clamp(seconds / duration * 100, 0, 100)); }
        }
        await process.WaitForExitAsync(token);
        if (process.ExitCode != 0) throw new InvalidOperationException($"ffmpegが終了コード {process.ExitCode} で失敗しました。");
        progress.Report(100);
    }

    private static async Task<string> CaptureAsync(string exe, string args)
    {
        using var p = Process.Start(new ProcessStartInfo(exe, args) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true }) ?? throw new InvalidOperationException($"{exe}を起動できません。");
        var stdout = await p.StandardOutput.ReadToEndAsync(); var stderr = await p.StandardError.ReadToEndAsync(); await p.WaitForExitAsync();
        if (p.ExitCode != 0) throw new InvalidOperationException(stderr.Trim()); return stdout;
    }
    private static string Q(string value) => $"\"{value.Replace("\"", "\\\"")}\"";
    private static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
}
