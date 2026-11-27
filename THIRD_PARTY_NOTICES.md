# Third-party notices

ClipX itself is distributed under the MIT License. The following components
retain their own copyrights and licenses.

## LibVLCSharp.WPF 3.10.0

- Project: https://code.videolan.org/videolan/LibVLCSharp
- License: GNU Lesser General Public License v2.1 or later
- Usage: dynamically linked WPF bindings for video preview

## VideoLAN.LibVLC.Windows 3.0.23.1

- Project: https://code.videolan.org/videolan/libvlc-nuget
- License: GNU Lesser General Public License v2.1 or later
- Usage: dynamically loaded native playback engine and plugins

The NuGet packages include the applicable native binaries. When distributing
ClipX, keep their license notices and allow replacement of the dynamically
linked libraries as required by the LGPL.

## FFmpeg

- Project: https://ffmpeg.org/
- License information: https://ffmpeg.org/legal.html
- Usage: separate `ffmpeg` and `ffprobe` processes for probing, export, and frame capture

FFmpeg binaries are not part of this repository. FFmpeg is LGPL-2.1-or-later
by default, but builds that enable GPL components (including common libx264
builds) are covered by GPL-2.0-or-later.

Releases bundle one pinned build so the exact binaries are reproducible:

- Build: BtbN FFmpeg-Builds, tag `autobuild-2026-11-27-12-42`,
  asset `ffmpeg-n8.1.3-41-ga8029b6528-win64-gpl-8.1.zip`
  (pinned download URL in `.github/workflows/release.yml`)
- Corresponding source: FFmpeg is open source; the asset name encodes the
  upstream version (n8.1.3) and upstream commit (`ga8029b6528`), and BtbN
  publishes the exact build parameters. The source can be obtained from
  git.ffmpeg.org / the FFmpeg documentation.

ClipX invokes `ffmpeg`/`ffprobe` as independent child processes using only
command-line arguments and files, so the GPL of the bundled binaries does not
extend to ClipX's own code, which remains under the MIT License. When
distributing the bundled binaries, keep the license files copied into
`tools/`, retain the copyright and license notices, and honor the GPL's
requirement to provide (or offer to provide) the corresponding source.
