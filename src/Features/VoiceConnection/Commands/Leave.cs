using Klang.Common;
using Klang.Common.Validators;
using Klang.Common.Voice;
using Klang.Features.AudioPlayer.Players;
using NetCord.Hosting.Services.ApplicationCommands;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace Klang.Features.VoiceConnection.Commands;

public sealed class Leave : ICommand
{
    public static void Map(IHost host) => host.AddSlashCommand("leave", "Stop playback and leave", Handler);
    
    private static async Task Handler(
        AudioPlayerManager playerManager,
        VoiceClientManager clientManager,
        ApplicationCommandContext context)
    {   
        if (!AudioCommandValidator.SameVoiceChannel(context, clientManager, out string error))
        {   
            await context.Interaction.SendResponseAsync(InteractionCallback.Message(error));
            return;
        }
        
        await context.Interaction.SendResponseAsync(InteractionCallback.DeferredMessage());
        
        await playerManager.RemoveAsync(context);
        
        await context.Interaction.DeleteResponseAsync();
    }
}
