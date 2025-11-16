using System.Text;
using AngleSharp.Text;
using Klang.Common;
using Klang.Common.Validators;
using NetCord.Hosting.Services.ApplicationCommands;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using YoutubeExplode;
using YoutubeExplode.Search;

namespace Klang.Features.Search.Commands.SearchList;

public sealed class SearchList : ICommand
{
    public static void Map(IHost host) => host.AddSlashCommand("search", "Search for a song", Handler);
    
    private static async Task Handler(YoutubeClient youtubeClient, ApplicationCommandContext context, string query)
    {   
        if (!CommandInputValidator.TrySanitizeQuery(query, out query, out string error))
        {   
            await context.Interaction.SendResponseAsync(InteractionCallback.Message(error));
            return;
        }
        
        await context.Interaction.SendResponseAsync(InteractionCallback.DeferredMessage());
        
        List<VideoSearchResult> results = new(10);
        await foreach (var video in youtubeClient.Search.GetVideosAsync(query))
        {
            results.Add(video);
            if (results.Count >= 10)
            {
                break;
            }
        }
        
        if (results.Count <= 0)
        {
            await context.Interaction.ModifyResponseAsync(message =>
            {
                message.Content = "No songs was found.";
            });
            return;
        }
        
        StringBuilder body = new();
        foreach (var result in results)
        {   
            body.AppendLine($"- {result.Title}");
        }
    
        await context.Interaction.ModifyResponseAsync(message =>
        {
            message.Content = $"**Top 10 search results**\n{body}";
        });
    }
}
