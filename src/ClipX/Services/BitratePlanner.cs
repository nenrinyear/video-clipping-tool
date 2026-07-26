namespace ClipX;

public sealed record EncodePlan(int VideoKbps, int AudioKbps, double ExpectedMegabytes, bool IsViable, string Advice);

public static class BitratePlanner
{
    public static EncodePlan Create(double durationSeconds, double targetMegabytes = 5.0, bool hasAudio = true)
    {
        if (durationSeconds <= 0) return new(0, 0, 0, false, "切り出す範囲を指定してください。");
        const double safety = 0.94; // mux overhead and bitrate variance
        var totalKbps = targetMegabytes * 1_000_000 * 8 * safety / durationSeconds / 1000;
        var audio = hasAudio ? (totalKbps >= 450 ? 96 : totalKbps >= 260 ? 64 : 48) : 0;
        var video = Math.Max(0, (int)Math.Floor(totalKbps - audio));
        var expected = (video + audio) * durationSeconds * 1000 / 8 / 1_000_000 / safety;
        var viable = video >= 120;
        var advice = !viable ? "この長さを5MBにすると画質が極端に低下します。範囲を短くしてください。"
            : video < 350 ? "かなり強い圧縮です。短くすると画質が改善します。"
            : video < 900 ? "SNS投稿向けの軽めの画質になります。"
            : "十分な画質を見込めます。";
        return new(video, audio, expected, viable, advice);
    }
}
