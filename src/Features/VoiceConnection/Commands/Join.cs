using Klang.Common;
using Klang.Common.Validators;
using Klang.Common.Voice;
using Klang.Features.AudioPlayer.Players;
using NetCord.Hosting.Services.ApplicationCommands;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace Klang.Features.VoiceConnection.Commands;

public sealed class Join : ICommand
{
    public static void Map(IHost host) => host.AddSlashCommand("join", "Make the bot join your voice channel", Handler);
    
    private static async Task Handler(
        VoiceClientManager clientManager,
        AudioPlayerManager playerManager,
        ApplicationCommandContext context)
    {
        if (!AudioCommandValidator.UserInVoiceChannel(context, out string error))
        {   
            await context.Interaction.SendResponseAsync(InteractionCallback.Message(error));
            return;
        }
        
        await context.Interaction.SendResponseAsync(InteractionCallback.DeferredMessage());
        
        if (playerManager.TryGet(context.Guild!.Id, out var player))
        {
            await player.StopAsync();
        }
        
        await clientManager.JoinVoiceChannelAsync(context);
        
        await context.Interaction.DeleteResponseAsync();
    }
}
