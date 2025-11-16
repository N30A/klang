using Klang;
using Klang.Common.Voice;
using Klang.Features.AudioPlayer.Players;
using NetCord.Hosting.Gateway;
using NetCord.Hosting.Services.ApplicationCommands;
using YoutubeExplode;

DotNetEnv.Env.Load();

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDiscordGateway(options =>
{
    options.Token = builder.Configuration.GetValue<string>("BOT_TOKEN");
});
builder.Services.AddSingleton<VoiceClientManager>();
builder.Services.AddSingleton<AudioPlayerManager>();
builder.Services.AddSingleton<YoutubeClient>();
builder.Services.AddApplicationCommands();

var host = builder.Build();

host.MapCommands();

await host.RunAsync();
