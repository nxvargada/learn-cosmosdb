using Microsoft.Azure.Cosmos;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.MapGet("/documents", () => {
    using (var client = new CosmosClient(endpoint, masterKey))
    {
        
    }
});

app.Run();
