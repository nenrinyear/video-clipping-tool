# Contributing to ClipX

Issue reports and pull requests are welcome.

## Development setup

1. Install the .NET 10 SDK on Windows 10 or 11.
2. Put `ffmpeg.exe` and `ffprobe.exe` in `tools/`, or add them to `PATH`.
3. Run `dotnet restore ClipX.sln`.
4. Run `dotnet build ClipX.sln`.

Keep UI text concise and test changes with at least one MP4 and one non-MP4
input. For export changes, verify both the 5 MB mode and stream-copy mode.

## Pull requests

- Keep each pull request focused on one concern.
- Explain user-visible behavior and validation performed.
- Do not commit generated `bin`, `obj`, `publish`, or FFmpeg binary files.
- Confirm that third-party additions are compatible with an MIT-licensed app
  and update `THIRD_PARTY_NOTICES.md`.
