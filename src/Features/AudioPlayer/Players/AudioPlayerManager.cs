using System.Collections.Concurrent;
using Klang.Common.Voice;
using System.Diagnostics.CodeAnalysis;

namespace Klang.Features.AudioPlayer.Players;

public class AudioPlayerManager
{
    private readonly ConcurrentDictionary<ulong, GuildAudioPlayer> _players = [];
    private readonly VoiceClientManager _voiceClientManager;
    
    public AudioPlayerManager(VoiceClientManager voiceClientManager)
    {
        _voiceClientManager = voiceClientManager;
    }
    
    public GuildAudioPlayer GetOrCreate(ulong guildId)
    {
        return _players.GetOrAdd(guildId, _ => new GuildAudioPlayer(_voiceClientManager));
    }

    public bool TryGet(ulong guildId, [MaybeNullWhen(false)] out GuildAudioPlayer player)
    {
        return _players.TryGetValue(guildId, out player);
    }

    public async Task RemoveAsync(ulong guildId)
    {
        if (_players.TryRemove(guildId, out _))
        {
            await _voiceClientManager.DisconnectAsync(guildId);
        }
    }
}
