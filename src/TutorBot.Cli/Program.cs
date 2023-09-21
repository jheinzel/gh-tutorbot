using System.Net;
using System.Reflection.PortableExecutable;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Octokit;
using Octokit.Internal;
using TutorBot;
using TutorBot.Commands;
using TutorBot.Infrastructure.OctokitExtensions;
using TutorBot.Utility;

var accessToken = await AuthHelper.GetPersonalAccessTokenAsync();

using IHost host = Host.CreateDefaultBuilder(args)
  .ConfigureServices(RegisterServices)
  .ConfigureServices(services => services
                       .AddLogging()
                       .AddSingleton<App>())
  .Build();

await host.Services.GetService<App>()!.RunAsync(args);


void RegisterServices(IServiceCollection services)
{
  services.AddSingleton<IGitHubClient>(OctokitExtensions.GetGitHubClient(Constants.APP_NAME, accessToken));
  services.AddSingleton<ConfigurationHelper>();
  services.RegisterCommands(typeof(Program).Assembly);
}