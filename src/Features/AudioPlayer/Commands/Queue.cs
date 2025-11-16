using System.Text;
using Klang.Common;
using Klang.Features.AudioPlayer.Players;
using NetCord.Hosting.Services.ApplicationCommands;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace Klang.Features.AudioPlayer.Commands;

public sealed class Queue : ICommand
{
    public static void Map(IHost host) => host.AddSlashCommand("queue", "Show the queue", Handler);

    private static async Task Handler(
        AudioPlayerManager audioPlayerManager,
        ApplicationCommandContext context
    )
    {
        await context.Interaction.SendResponseAsync(InteractionCallback.DeferredMessage());
        
        if (!audioPlayerManager.TryGet(context.Guild!.Id, out var player))
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
                message.Content = "The queue is empty.";
            });
            return;
        }
        
        StringBuilder body = new();
        for (int i = 0; i < player.Queue.Count; i++)
        {   
            var track = player.Queue[i];
            body.AppendLine($"{i+1}. {track.Title}");
        }
    
        await context.Interaction.ModifyResponseAsync(message =>
        {
            message.Content = $"**Queue**\n{body}";
        });
    }
}
