# ClipX

ClipXは、Windows上で動画をプレビューしながら切り出し、Xへの投稿向けに5 MB以内を狙って圧縮できるデスクトップアプリです。

## Features

- LibVLCによる元ファイルの直接再生
- 音声再生、ハードウェアデコード、シーク
- 再生位置とトリミング範囲を重ねた統合タイムライン
- ドラッグ＆ドロップ、時刻入力、現在位置からの範囲指定
- 5 MB以内を狙った映像・音声ビットレートの自動計算
- ビットレートに応じた解像度調整、H.264/AAC、faststart
- 再エンコードしない高速トリミング
- 出力サイズ概算と処理進捗

プレビューはWindows Media PlayerやOSの動画コーデックに依存しません。VideoLAN LibVLCをアプリ内で動的に利用し、元ファイルを直接再生します。

## Requirements

- Windows 10またはWindows 11（x64推奨）
- 開発時は.NET 10 SDK
- 実行時は.NET 10 Desktop Runtime、またはself-containedビルド
- `ffmpeg.exe`と`ffprobe.exe`

FFmpegの実行ファイルは、ClipXと同じ場所の`tools`フォルダーへ配置するか、PATHを通してください。

```text
ClipX.exe
LibVLCSharp.dll
LibVLCSharp.WPF.dll
libvlc/
tools/
  ffmpeg.exe
  ffprobe.exe
```

## Development

```powershell
git clone <repository-url>
cd ClipX
dotnet restore ClipX.sln --configfile NuGet.Config
dotnet build ClipX.sln
dotnet run --project src/ClipX/ClipX.csproj
```

主要コードは責務別に配置しています。

```text
src/ClipX/
  Controls/    統合タイムラインなどのUIコントロール
  Models/      メディア情報モデル
  Services/    FFmpeg連携と圧縮計画
  MainWindow.* 画面と再生操作
```

## Publishing

フレームワーク依存のx64ビルド：

```powershell
dotnet publish src/ClipX/ClipX.csproj -c Release -r win-x64 --self-contained false -o publish
```

self-containedのx64ビルド：

```powershell
dotnet publish src/ClipX/ClipX.csproj -c Release -r win-x64 --self-contained true -o publish
```

LibVLCのDLLと`libvlc`フォルダーはpublish出力へ自動的に含まれます。FFmpegを同梱する場合は、使用するビルドのライセンス条件とソース提供義務を確認してください。

## 5 MB mode

映像、音声、MP4コンテナを含めて5,000,000 bytes未満になるよう、6%の安全余裕を取ってビットレートを計算します。エンコーダーの誤差があるため厳密な保証ではありません。必要な映像ビットレートが低すぎる場合は、画質劣化を避けるため範囲短縮を案内します。

## License

ClipXのコードは[MIT License](LICENSE)で公開しています。

LibVLCSharpとLibVLCはLGPL-2.1-or-laterです。FFmpegはビルド構成によってLGPLまたはGPLになります。再配布時の詳細は[THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md)を確認してください。

コントリビューションについては[CONTRIBUTING.md](CONTRIBUTING.md)を参照してください。
