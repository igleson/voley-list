using Dapper;
using VolleyList.WebApi.Config;
using VolleyList.WebApi.Controllers;
using VolleyList.WebApi.Database;
using VolleyList.WebApi.Models;

DefaultTypeMap.MatchNamesWithUnderscores = true;

//load .env file
DotEnv.Load(Path.Combine(Directory.GetCurrentDirectory(), ".env"));

var builder = WebApplication.CreateBuilder(args);
// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

#if DEBUG
builder.Services.AddSingleton<IDbConnectionProvider, SqliteConnectionProvider>();
#else
builder.Services.AddSingleton<IDbConnectionProvider, SupabaseConnectionProvider>();
#endif


builder.Services.AddSingleton<DatabaseContext>();

builder.Services.AddSingleton<Storage>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();


app.MapPost("/listing", ListingController.CreateListingAsync)
    .Accepts<CreateListingRequest>(contentType: "application/json")
    .Produces<Listing>(contentType: "application/json");

app.MapPost("/listing/{listingId}/add", ListingController.AddParticipantAsync)
    .Accepts<AddParticipantRequest>(contentType: "application/json")
    .Produces<ListingEvent>(contentType: "application/json");

app.MapDelete("/listing/{listingId}/remove/{participantId}", ListingController.RemoveParticipantAsync);

app.MapGet("/listing/{listingId}", ListingController.ReadListingAsync)
    .Produces<Listing>(contentType: "application/json");
app.Run();