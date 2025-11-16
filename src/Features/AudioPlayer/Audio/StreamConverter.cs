using System.Diagnostics;
using NetCord.Gateway.Voice;
using YoutubeExplode.Videos.Streams;

namespace Klang.Features.AudioPlayer.Audio;

public static class StreamConverter
{
    public static async Task ToOpusStream(
        IStreamInfo streamInfo,
        Stream outStream,
        CancellationToken cancellationToken = default
    )
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            ArgumentList =
            {
                "-reconnect", "1",
                "-reconnect_streamed", "1",
                "-reconnect_delay_max", "5",
                "-i", streamInfo.Url,
                "-vn",
                "-loglevel", "quiet",
                "-ac", "2",
                "-f", "s16le",
                "-ar", "48000",
                "pipe:1",
            },
            RedirectStandardOutput = true,
            UseShellExecute = false
        };

        var process = Process.Start(startInfo)!;
        var pcmStream = process.StandardOutput.BaseStream;
        
        await using var opusStream = new OpusEncodeStream(
            outStream,
            PcmFormat.Short,
            VoiceChannels.Stereo,
            OpusApplication.Audio
        );

        try
        {
            await pcmStream.CopyToAsync(opusStream, cancellationToken);
            await opusStream.FlushAsync(cancellationToken);
        }
        catch (OperationCanceledException) {}
        finally
        {
            if (!process.HasExited)
            {
                process.Kill();
            }
        }
    }
}
