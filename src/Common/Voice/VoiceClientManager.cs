using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Klang.Common.Voice.Exceptions;
using NetCord.Gateway;
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
            || voiceState.ChannelId is not { } userChannelId
        )
        {
            throw new NotInVoiceChannelException("You are not in a voice channel.");
        }

        if (_clients.TryGetValue(guild.Id, out var existingClient))
        {
            ulong botChannelId = context.Guild!.VoiceStates[context.Client.Id].ChannelId!.Value;
            if (userChannelId != botChannelId)
            {
                try { await existingClient.CloseAsync(); }
                finally { existingClient.Dispose(); }
                
                _clients.TryRemove(guild.Id, out _);
            }
            else
            {
                return existingClient;
            }
        }
        
        var client = await context.Client.JoinVoiceChannelAsync(guild.Id, userChannelId);
        
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
    
    public async Task DisconnectAsync(ApplicationCommandContext context)
    {   
        var guild = context.Guild!;
        
        if (_clients.TryRemove(guild.Id, out var client))
        {   
            try { await client.CloseAsync(); }
            finally { client.Dispose(); }
        }
        
        await context.Client.UpdateVoiceStateAsync(new VoiceStateProperties(guild.Id, null));
    }
}
