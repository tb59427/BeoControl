using BeoControlBlazor.Components;

using BeoControlBlazorServices;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Register DeviceService as a normal singleton; this host drives its lifecycle explicitly.
builder.Services.AddSingleton<DeviceService>();
builder.Services.AddScoped<BlazorSpotifyService>();
builder.Services.AddScoped<ISpotifyService>(sp => sp.GetRequiredService<BlazorSpotifyService>());
builder.Services.AddSingleton<IAutostartRegistrationService, UnsupportedAutostartRegistrationService>();

var app = builder.Build();
var deviceService = app.Services.GetRequiredService<DeviceService>();
await deviceService.AutoConnectAsync();
app.Lifetime.ApplicationStopping.Register(() => deviceService.Disconnect(silent: true));

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}



//app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();

// Small HTTP API for local automation (e.g. a menu bar app switching the PC2 to the local input
// during a call). Same access as the web UI: no authentication.
app.MapGet("/api/status", (DeviceService ds) => Results.Ok(new
{
    connected = ds.IsConnected,
    device = ds.CurrentDevice?.Type.ToString(),
    pc2 = ds.Pc2Routing,
})).DisableAntiforgery();

app.MapPost("/api/command/{cmd}", (string cmd, string? arg, DeviceService ds) =>
{
    if (!ds.IsConnected)
        return Results.Problem("No device connected", statusCode: 503);
    ds.SendCommand(cmd, arg);
    return Results.Ok(new { ok = true, pc2 = ds.Pc2Routing });
}).DisableAntiforgery();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapFallbackToFile("index.html"); // if Blazor WASM or Blazor Web App
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.Run();
