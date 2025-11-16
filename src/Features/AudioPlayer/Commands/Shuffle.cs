using Klang.Common;
using Klang.Common.Validators;
using Klang.Common.Voice;
using Klang.Features.AudioPlayer.Players;
using NetCord.Hosting.Services.ApplicationCommands;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace Klang.Features.AudioPlayer.Commands;

public sealed class Shuffle : ICommand
{
    public static void Map(IHost host) => host.AddSlashCommand("shuffle", "Shuffle the queue", Handler);
    
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

        if (player!.Queue.Count <= 0)
        {   
            await context.Interaction.ModifyResponseAsync(message =>
            {
                message.Content = "Nothing to shuffle since the queue is empty.";
            });
            return;
        }
        
        await player.ShuffleAsync();
        
        await context.Interaction.ModifyResponseAsync(message =>
        {
            message.Content = "Shuffled the queue.";
        });
    }
}