using Madar.Client;
using Madar.Client.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(_ => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});
builder.Services.AddMudServices();
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<MadarApiClient>();
builder.Services.AddScoped<CaseSearchApiClient>();
builder.Services.AddScoped<MadarAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(services =>
    services.GetRequiredService<MadarAuthenticationStateProvider>());

await builder.Build().RunAsync();
