using Klang.Common;
using Klang.Common.Validators;
using Klang.Common.Voice;
using Klang.Features.AudioPlayer.Players;
using NetCord.Hosting.Services.ApplicationCommands;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace Klang.Features.AudioPlayer.Commands;

public sealed class Clear : ICommand 
{
    public static void Map(IHost host) => host.AddSlashCommand("clear", "Clear the queue", Handler);
    
    private static async Task Handler(
        AudioPlayerManager audioPlayerManager,
        VoiceClientManager voiceClientManager,
        ApplicationCommandContext context
    )
    {
        await context.Interaction.SendResponseAsync(InteractionCallback.DeferredMessage());
        
        var guild = context.Guild!;
        
        if (!AudioCommandValidator.SameVoiceChannel(context, voiceClientManager, out string error))
        {
            await context.Interaction.ModifyResponseAsync(message => message.Content = error);
            return;
        }
        
        if (!audioPlayerManager.TryGet(guild.Id, out var player))
        {   
            await context.Interaction.ModifyResponseAsync(message =>
            {
                message.Content = "No queue since nothing is playing.";
            });
            return;
        }
        
        if (player.Queue.Count <= 0)
        {
            await context.Interaction.ModifyResponseAsync(message =>
            {
                message.Content = "The queue is already empty.";
            });
            return;
        }
        
        await player.ClearAsync();
        
        await context.Interaction.ModifyResponseAsync(message =>
        {
            message.Content = "The queue is now cleared.";
        });
    }
}
