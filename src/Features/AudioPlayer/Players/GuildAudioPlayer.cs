using System.Collections.Concurrent;
using Klang.Common.Models;
using Klang.Common.Voice;
using Klang.Features.AudioPlayer.Audio;
using NetCord.Services.ApplicationCommands;

namespace Klang.Features.AudioPlayer.Players;

public class GuildAudioPlayer
{   
    private readonly ulong _guildId;
    private readonly VoiceClientManager _clientManager;
    private readonly ConcurrentQueue<AudioTrack> _queue = [];
    private CancellationTokenSource? _currentCts;
    
    public GuildAudioPlayer(ulong guildId, VoiceClientManager clientManager)
    {
        _guildId = guildId;
        _clientManager = clientManager;
    }
    
    public AudioTrack? CurrentTrack { get; private set; }
    public bool IsPlaying { get; private set; }
    
    public IReadOnlyList<AudioTrack> Queue => _queue.ToList().AsReadOnly();

    public Task ClearAsync()
    {
        _queue.Clear();
        return Task.CompletedTask;
    } 

    public Task EnqueueAsync(AudioTrack track)
    {
        _queue.Enqueue(track);
        return Task.CompletedTask;
    }
    
    public async Task PlayAsync(AudioTrack track, ApplicationCommandContext context)
    {   
        await EnqueueAsync(track);
        
        if (IsPlaying)
        {
            return; 
        }
        
        IsPlaying = true;
        _ = PlayLoopAsync(context);
    }

    private async Task PlayLoopAsync(ApplicationCommandContext context)
    {
        try
        {
            var client = await _clientManager.JoinVoiceChannelAsync(context);
            
            await using var stream = client.CreateOutputStream();
            
            while (_queue.TryDequeue(out var track))
            {
                CurrentTrack = track;
                
                _currentCts?.CancelAsync();
                _currentCts?.Dispose();
                _currentCts = new CancellationTokenSource();
                var token = _currentCts.Token;
                
                try
                {
                    await StreamConverter.ToOpusStream(track.Stream, stream, token);
                    await stream.FlushAsync(token);
                }
                catch (OperationCanceledException) {}
            }

            CurrentTrack = null;
        }
        finally
        {   
            _currentCts?.CancelAsync();
            _currentCts?.Dispose();
            _currentCts = null;
            IsPlaying = false;
        }
    }
    
    public Task SkipAsync()
    {
        if (!IsPlaying)
        {
            return Task.CompletedTask;
        }
        
        _currentCts?.Cancel();
        return Task.CompletedTask;
    }
    
    public Task StopAsync()
    {
        _queue.Clear();
        _currentCts?.Cancel();
        return Task.CompletedTask;
    }

    public Task ShuffleAsync()
    {   
        Random random = new();
        List<AudioTrack> newQueue = _queue.ToList();
        
        for (int i = newQueue.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (newQueue[i], newQueue[j]) = (newQueue[j], newQueue[i]);
        }
        
        _queue.Clear();
        
        foreach (var track in newQueue)
        {
            _queue.Enqueue(track);
        }
        
        return Task.CompletedTask;
    }
}
