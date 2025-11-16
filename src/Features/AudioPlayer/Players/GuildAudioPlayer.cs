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
            await foreach (var command in _commandChannel.Reader.ReadAllAsync())
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
            _client?.Dispose();
            IsPlaying = false;
            CurrentTrack = null;
        }
    }
    
    private async Task EnsurePlaybackLoopStartedAsync(ApplicationCommandContext context)
    {
        if (_playbackTask != null)
        {
            return;
        }
        
        _client ??= await _clientManager.JoinVoiceChannelAsync(context);
        _playbackTask = Task.Run(() => PlayLoopAsync(_client));
    }
    
    private async Task PlayLoopAsync(VoiceClient client)
    {
        try
        {
            await using var stream = client.CreateOutputStream();

            while (true)
            {
                if (!_queue.TryDequeue(out var track))
                {   
                    IsPlaying = false;
                    CurrentTrack = null;
                    await Task.Delay(100);
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
}
