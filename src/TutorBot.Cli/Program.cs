using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TutorBot;
using TutorBot.Commands;
using TutorBot.Infrastructure.OctokitExtensions;
using TutorBot.Utility;

var accessToken = await AuthHelper.GetPersonalAccessTokenAsync();

using IHost host = Host.CreateDefaultBuilder(args)
  .ConfigureServices(RegisterServices)
  .ConfigureServices(services => services
                       .AddLogging(config => config.SetMinimumLevel(LogLevel.Error))
                       .AddSingleton<App>())
  .Build();

await host.Services.GetService<App>()!.RunAsync(args);


void RegisterServices(IServiceCollection services)
{
  services.AddSingleton<IGitHubClassroomClient>(new GitHubClassroomClient(Constants.APP_NAME, accessToken));
  services.AddSingleton<ConfigurationHelper>();
  services.RegisterCommands(typeof(Program).Assembly);
}