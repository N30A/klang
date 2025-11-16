using Klang.Common;
using Klang.Common.Models;
using Klang.Common.Validators;
using Klang.Common.Voice;
using Klang.Features.AudioPlayer.Players;
using NetCord.Hosting.Services.ApplicationCommands;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using YoutubeExplode;
using YoutubeExplode.Search;
using YoutubeExplode.Videos.Streams;

namespace Klang.Features.AudioPlayer.Commands;

public sealed class Play : ICommand
{
    public static void Map(IHost host) => host.AddSlashCommand("play", "Play a song", Handler);
    
    private static async Task Handler(
        VoiceClientManager voiceClientManager,
        AudioPlayerManager audioPlayerManager,
        YoutubeClient youtubeClient,
        ApplicationCommandContext context,
        string query
    )
    {    
        if (!CommandInputValidator.TrySanitizeQuery(query, out query, out string error))
        {   
            await context.Interaction.SendResponseAsync(InteractionCallback.Message(error));
            return;
        }
        
        if (!AudioCommandValidator.UserInVoiceChannel(context, out error))
        {   
            await context.Interaction.SendResponseAsync(InteractionCallback.Message(error));
            return;
        }
        
        await context.Interaction.SendResponseAsync(InteractionCallback.DeferredMessage());
        
        VideoSearchResult? result = null;
        await foreach (var video in youtubeClient.Search.GetVideosAsync(query))
        {
            result = video;
            break;
        }
        
        if (result == null)
        {
            await context.Interaction.ModifyResponseAsync(message =>
            {
                message.Content = "No song was found.";
            });
            return;
        }
        
        var manifest = await youtubeClient.Videos.Streams.GetManifestAsync(result.Id);
        var songStream = manifest
            .GetAudioOnlyStreams()
            .Where(stream => stream.Container == Container.WebM)
            .GetWithHighestBitrate();

        var audioTrack = new AudioTrack
        {
            Title = result.Title,
            Url = result.Url,
            Stream = songStream
        };
        
        var player = audioPlayerManager.GetOrCreate(context.Guild!.Id);
        
        bool wasPlaying = player.IsPlaying;
        
        await player.PlayAsync(audioTrack, context);
        
        string status = wasPlaying ? "Queued" : "Now playing";
        await context.Interaction.ModifyResponseAsync(message =>
        {
            message.Content = $"[\u2761]({audioTrack.Url}) {status} **{audioTrack.Title}**";
        });
    }
}
