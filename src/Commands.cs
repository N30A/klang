using Klang.Common;
using Klang.Features.AudioPlayer.Commands;
using Klang.Features.Search.Commands.SearchList;

namespace Klang;

public static class Commands
{
    public static void MapCommands(this IHost host)
    {
        host.MapCommand<SearchList>();
        host.MapCommand<Play>();
        host.MapCommand<Queue>();
        host.MapCommand<Playing>();
        host.MapCommand<Clear>();
        host.MapCommand<Shuffle>();
        host.MapCommand<Skip>();
        host.MapCommand<Stop>();
    }
    
    private static IHost MapCommand<TCommand>(this IHost host) where TCommand : ICommand
    {
        TCommand.Map(host);
        return host;
    }
}
