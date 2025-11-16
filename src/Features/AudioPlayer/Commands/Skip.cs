using Klang.Common;
using Klang.Common.Validators;
using Klang.Common.Voice;
using Klang.Features.AudioPlayer.Players;
using NetCord.Hosting.Services.ApplicationCommands;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace Klang.Features.AudioPlayer.Commands;

public sealed class Skip : ICommand
{
    public static void Map(IHost host) => host.AddSlashCommand("skip", "Skip the currently playing track", Handler);
    
    private static async Task Handler(
        AudioPlayerManager audioPlayerManager,
        VoiceClientManager voiceClientManager,
        ApplicationCommandContext context
    )
    {
        await context.Interaction.SendResponseAsync(InteractionCallback.DeferredMessage());

        if (!AudioCommandValidator.SameVoiceChannel(context, voiceClientManager, out string error))
        {
            await context.Interaction.ModifyResponseAsync(message => message.Content = error);
            return;
        }
        
        audioPlayerManager.TryGet(context.Guild!.Id, out var player);
        
        var currentTrack = player!.CurrentTrack;
        
        if (currentTrack == null)
        {   
            await context.Interaction.ModifyResponseAsync(message => message.Content = "Nothing is playing.");
            return;
        }
        
        await player.SkipAsync();
        
        await context.Interaction.ModifyResponseAsync(message =>
        {
            message.Content = $"Skipped **{currentTrack.Title}**";
        });
    }
}