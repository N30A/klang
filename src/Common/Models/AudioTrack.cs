using YoutubeExplode.Videos.Streams;

namespace Klang.Common.Models;

public class AudioTrack
{
    public string Title { get; set; }
    public string Url { get; set; }
    public IStreamInfo Stream { get; set; }
}
