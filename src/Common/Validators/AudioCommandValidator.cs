using Klang.Common.Voice;
using NetCord.Gateway;
using NetCord.Services.ApplicationCommands;

namespace Klang.Common.Validators;

public static class AudioCommandValidator
{
    public static bool UserInVoiceChannel(ApplicationCommandContext context, out string error)
    {
        var guild = context.Guild!;
        
        if (!guild.VoiceStates.TryGetValue(context.User.Id, out _))
        {
            error = "You must be in a voice channel to use this command.";
            return false;
        }
        
        error = string.Empty;
        return true;
    }
    
    public static bool BotInVoiceChannel(VoiceClientManager voiceClientManager, ulong guildId, out string error)
    {
        if (!voiceClientManager.TryGet(guildId, out _))
        {
            error = "Command not usable when I'm not in a voice channel.";
            return false;
        }
        
        error = string.Empty;
        return true;
    }
    
    public static bool SameVoiceChannel(
        ApplicationCommandContext context,
        VoiceClientManager voiceClientManager,
        out string error
    )
    {   
        var guild = context.Guild!;

        if (!UserInVoiceChannel(context, out error))
        {
            return false;
        }
        
        VoiceState userVoiceState = guild.VoiceStates[context.User.Id];
        ulong userChannelId = userVoiceState.ChannelId!.Value;

        if (!voiceClientManager.TryGet(guild.Id, out var botVoiceClient))
        {
            error = "Command not usable when I'm not in a voice channel.";
            return false;
        }
        
        var botVoiceState = guild.VoiceStates.Values.FirstOrDefault(state => state.UserId == botVoiceClient.UserId);

        if (botVoiceState == null || botVoiceState.ChannelId != userChannelId)
        {
            error = "You must be in the same voice channel.";
            return false;
        }
        
        error = string.Empty;
        return true;
    }
}
