using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Klang.Common.Voice.Exceptions;
using NetCord.Gateway.Voice;
using NetCord.Services.ApplicationCommands;

namespace Klang.Common.Voice;

public class VoiceClientManager
{
    private readonly ConcurrentDictionary<ulong, VoiceClient> _clients = [];
    
    public async Task<VoiceClient> JoinVoiceChannelAsync(ApplicationCommandContext context)
    {
        var guild = context.Guild!;
        
        if (!guild.VoiceStates.TryGetValue(context.User.Id, out var voiceState)
            || voiceState.ChannelId is not { } channelId
        )
        {
            throw new NotInVoiceChannelException("You are not in a voice channel.");
        }

        if (_clients.TryGetValue(guild.Id, out var existingClient))
        {
            return existingClient;
        }
        
        var client = await context.Client.JoinVoiceChannelAsync(guild.Id, channelId);
        
        await client.StartAsync();
        await client.EnterSpeakingStateAsync(
            new SpeakingProperties(SpeakingFlags.Microphone)
        );
        
        _clients[guild.Id] = client;
        
        return client;
    }

    public bool TryGet(ulong guildId, [MaybeNullWhen(false)] out VoiceClient client)
    {
        return _clients.TryGetValue(guildId, out client);
    }
    
    public async Task DisconnectAsync(ulong guildId)
    {
        if (_clients.TryRemove(guildId, out var client))
        {
            await client.CloseAsync();
        }
    }
}
