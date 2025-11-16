using Klang.Common.Models;
using NetCord.Services.ApplicationCommands;

namespace Klang.Features.AudioPlayer.Players;

public abstract record PlayerCommand
{
    public sealed record Play(AudioTrack Track, ApplicationCommandContext Context) : PlayerCommand;
    public sealed record Stop() : PlayerCommand;    
    public sealed record Skip() : PlayerCommand;    
    public sealed record Shuffle() : PlayerCommand;    
    public sealed record Clear() : PlayerCommand;    
}
