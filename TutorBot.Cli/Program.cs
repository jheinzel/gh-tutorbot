using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Octokit;
using TutorBot;
using TutorBot.Commands;
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
  GitHubClient client = new GitHubClient(new ProductHeaderValue(Constants.APP_NAME));
  client.Credentials = new Credentials(accessToken);

  services.AddSingleton<IGitHubClient>(client);
  services.AddSingleton<ConfigurationHelper>();
  services.RegisterCommands(typeof(Program).Assembly);
}
