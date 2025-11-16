using System.Collections.Concurrent;
using System.Threading.Channels;
using Klang.Common.Models;
using Klang.Common.Voice;
using Klang.Features.AudioPlayer.Audio;
using NetCord.Gateway.Voice;
using NetCord.Services.ApplicationCommands;

namespace Klang.Features.AudioPlayer.Players;

public class GuildAudioPlayer
{   
    private readonly VoiceClientManager _clientManager;
    private readonly ConcurrentQueue<AudioTrack> _queue = [];
    private readonly Channel<PlayerCommand> _commandChannel = Channel.CreateUnbounded<PlayerCommand>();
    
    private readonly CancellationTokenSource _shutdownCts = new();
    private CancellationTokenSource _playbackLoopCts = new();
    private CancellationTokenSource? _currentCts;
    private VoiceClient? _client;
    private Task? _playbackTask;
    
    public AudioTrack? CurrentTrack { get; private set; }
    public bool IsPlaying { get; private set; }
    public IReadOnlyList<AudioTrack> Queue => _queue.ToList().AsReadOnly();
    
    public GuildAudioPlayer(VoiceClientManager clientManager)
    {
        _clientManager = clientManager;
        _ = RunAsync();
    }
    
    public ValueTask PlayAsync(AudioTrack track, ApplicationCommandContext context)
    {
        return _commandChannel.Writer.WriteAsync(new PlayerCommand.Play(track, context));
    }
    public ValueTask StopAsync() => _commandChannel.Writer.WriteAsync(new PlayerCommand.Stop());
    public ValueTask SkipAsync() => _commandChannel.Writer.WriteAsync(new PlayerCommand.Skip());
    public ValueTask ShuffleAsync() => _commandChannel.Writer.WriteAsync(new PlayerCommand.Shuffle());
    public ValueTask ClearAsync() => _commandChannel.Writer.WriteAsync(new PlayerCommand.Clear());
    
    private async Task RunAsync()
    {
        try
        {
            await foreach (var command in _commandChannel.Reader.ReadAllAsync(_shutdownCts.Token))
            {
                switch (command)
                {
                    case PlayerCommand.Play(var track, var context):
                        await HandlePlayAsync(track, context);
                        break;
                    
                    case PlayerCommand.Stop:
                        HandleStop();
                        break;
                    
                    case PlayerCommand.Skip:
                        HandleSkip();
                        break;
                    case PlayerCommand.Shuffle:
                    
                        HandleShuffle();
                        break;
                    
                    case PlayerCommand.Clear:
                        HandleClear();
                        break;
                }
            }
        }
        finally
        {
            _currentCts?.Cancel();
            _currentCts?.Dispose();
            IsPlaying = false;
            CurrentTrack = null;
        }
    }
    
    private async Task EnsurePlaybackLoopStartedAsync(ApplicationCommandContext context)
    {   
        var client = await _clientManager.JoinVoiceChannelAsync(context);
        
        if (_playbackTask is { IsCompleted: false } && ReferenceEquals(client, _client))
        {
            return;
        }
        
        _playbackLoopCts.Cancel();
        try
        {
            if (_playbackTask != null)
            {
                await _playbackTask;
            }
        } catch (OperationCanceledException) {}
        
        _playbackLoopCts.Dispose();
        _playbackLoopCts = new CancellationTokenSource();
        
        _client = client;
        _playbackTask = Task.Run(() => PlayLoopAsync(_client, _playbackLoopCts.Token));
    }
    
    private async Task PlayLoopAsync(VoiceClient client, CancellationToken loopToken)
    {
        try
        {   
            await using var stream = client.CreateOutputStream();
            
            while (!_shutdownCts.IsCancellationRequested && !loopToken.IsCancellationRequested)
            {
                if (!_queue.TryDequeue(out var track))
                {   
                    IsPlaying = false;
                    CurrentTrack = null;
                    await Task.Delay(100, _shutdownCts.Token);
                    continue;
                }
                
                CurrentTrack = track;
                IsPlaying = true;
                
                _currentCts?.Cancel();
                _currentCts?.Dispose();
                _currentCts = new CancellationTokenSource();
                var token = _currentCts.Token;

                try
                {   
                    await StreamConverter.ToOpusStream(track.Stream, stream, token);
                    await stream.FlushAsync(token);
                }
                catch (OperationCanceledException) {}
                catch (Exception) {}
                finally
                {
                    CurrentTrack = null;
                    IsPlaying = false;
                }
            }
        }
        finally
        {   
            _currentCts?.Cancel();
            _currentCts?.Dispose();
            _currentCts = null;
        }
    }
    
    private async Task HandlePlayAsync(
        AudioTrack track,
        ApplicationCommandContext context
    )
    {   
       _queue.Enqueue(track);
       await EnsurePlaybackLoopStartedAsync(context);
    }
    
    private void HandleStop()
    {
        _queue.Clear();
        _currentCts?.Cancel();
    }
    
    private void HandleSkip() => _currentCts?.Cancel();
    
    private void HandleShuffle()
    {
        if (_queue.Count <= 1)
        {
            return;
        }
        
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
    }
    
    private void HandleClear() => _queue.Clear();

    public async Task CloseAsync()
    {
        try
        {
            _shutdownCts.Cancel();
            _playbackLoopCts.Cancel();
            _currentCts?.Cancel();
            _commandChannel.Writer.TryComplete();

            if (_playbackTask != null)
            {
                try { await _playbackTask; }
                catch (OperationCanceledException) {}
            }
        }
        finally
        {
            _currentCts?.Dispose();
            _playbackLoopCts?.Dispose();
            _shutdownCts.Dispose();
            _client = null;
            _playbackTask = null;
            _queue.Clear();
            IsPlaying = false;
            CurrentTrack = null;
        }
    }
}
