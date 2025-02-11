using Dapper;
using HtmxUi;
using HtmxUi.Controllers;
using VolleyList.Database;
using VolleyList.Services;

DefaultTypeMap.MatchNamesWithUnderscores = true;

var builder = WebApplication.CreateBuilder(args);

#if DEBUG
builder.Services.AddSingleton<IDbConnectionProvider, SqliteConnectionProvider>();
#else
builder.Services.AddSingleton<IDbConnectionProvider, SupabaseConnectionProvider>();
#endif


builder.Services.AddSingleton<DatabaseContext>();

builder.Services.AddSingleton<Storage>();

builder.Services.AddSingleton<ListingService>();

var app = builder.Build();

Templates.Init();

app.MapGet("/index.html", HtmxListingController.Index).DisableAntiforgery();


app.MapPost("/listings", HtmxListingController.CreateListing).DisableAntiforgery();

app.MapGet("/listings/{id}", HtmxListingController.DisplayListing).DisableAntiforgery();

app.MapPost("/listings/{Id}/players",  HtmxListingController.AddPlayer).DisableAntiforgery();

app.MapDelete("/listings/{Id}/players/{playerName}",  HtmxListingController.RemovePlayer).DisableAntiforgery();

app.Run();