using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using HslBikeApp;
using HslBikeApp.Services;
using HslBikeApp.State;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configuration
var config = builder.Configuration;
var apiKey = config["DigitransitSubscriptionKey"] ?? "";
var aggregatorBaseUrl = config["AggregatorBaseUrl"] ?? "https://kuoste.github.io/hsl-bike-data-aggregator";
var snapshotUrl = config["SnapshotUrl"] ?? $"{builder.HostEnvironment.BaseAddress}data/snapshots.json";

// HttpClient with Digitransit API key
var digitransitHttp = new HttpClient();
if (!string.IsNullOrEmpty(apiKey))
    digitransitHttp.DefaultRequestHeaders.Add("digitransit-subscription-key", apiKey);

// Plain HttpClient for other services
var plainHttp = new HttpClient();

builder.Services.AddSingleton(new StationService(digitransitHttp));
builder.Services.AddSingleton(new HistoryService(plainHttp, aggregatorBaseUrl));
builder.Services.AddSingleton(new CycleLaneService(plainHttp));
builder.Services.AddSingleton(new SnapshotService(plainHttp, snapshotUrl));
builder.Services.AddSingleton<AppState>();

await builder.Build().RunAsync();
