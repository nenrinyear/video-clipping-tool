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
- Usage: separate `ffmpeg` and `ffprobe` processes for probing and export

FFmpeg binaries are not part of this repository. FFmpeg is LGPL-2.1-or-later
by default, but builds that enable GPL components (including common libx264
builds) are covered by GPL-2.0-or-later. Anyone bundling FFmpeg with a ClipX
release is responsible for preserving its notices, providing corresponding
source where required, and satisfying the license of that exact build.
