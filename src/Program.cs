using Klang;
using Klang.Common.Voice;
using Klang.Features.AudioPlayer.Players;
using NetCord.Hosting.Gateway;
using NetCord.Hosting.Services.ApplicationCommands;
using YoutubeExplode;

if (Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") == "Development")
{
    DotNetEnv.Env.Load();
}

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDiscordGateway(options =>
{
    options.Token = builder.Configuration.GetValue<string>("BOT_TOKEN")
                    ?? throw new InvalidOperationException("BOT_TOKEN env variable is missing");
});
builder.Services.AddSingleton<VoiceClientManager>();
builder.Services.AddSingleton<AudioPlayerManager>();
builder.Services.AddSingleton<YoutubeClient>();
builder.Services.AddApplicationCommands();

var host = builder.Build();

host.MapCommands();

await host.RunAsync();
