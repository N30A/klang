using Klang.Common;
using Klang.Common.Voice;
using Klang.Features.AudioPlayer.Players;
using NetCord.Hosting.Services.ApplicationCommands;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace Klang.Features.AudioPlayer.Commands;

public sealed class Playing : ICommand
{
    public static void Map(IHost host) => host.AddSlashCommand("playing", "Show what's currently playing", Handler);
    
    private static async Task Handler(
        AudioPlayerManager audioPlayerManager,
        VoiceClientManager voiceClientManager,
        ApplicationCommandContext context
    )
    {
        await context.Interaction.SendResponseAsync(InteractionCallback.DeferredMessage());
        
        if (!audioPlayerManager.TryGet(context.Guild!.Id, out var player))
        {   
            await context.Interaction.ModifyResponseAsync(message =>
            {
                message.Content = "Waiting for you to play something!";
            });
            return;
        }
        
        if (player.CurrentTrack == null)
        {   
            await context.Interaction.ModifyResponseAsync(message => message.Content = "Nothing is playing.");
            return;
        }
        
        var track = player.CurrentTrack;
        await context.Interaction.ModifyResponseAsync(message =>
        {
            message.Content = $"[\u2761]({track.Url}) Currently playing **{track.Title}**";
        });
    }
}
