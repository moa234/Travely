using Dapper;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("TravelyConnection");
builder.Services.AddAntiforgery();

var travelRepository = new TravelyRepository(connectionString ?? string.Empty);
await travelRepository.CreateDatabaseTable();
builder.Services.AddSingleton(travelRepository);


var app = builder.Build();
app.UseAntiforgery();
app.UseStaticFiles();

app.MapFallbackToFile("html/page.html");

app.MapGet("/antiforgery", (IAntiforgery antiforgery, HttpContext context) =>
{
    var token = antiforgery.GetAndStoreTokens(context);
    var html = $"""<input name="{token.FormFieldName}" type="hidden" value="{token.RequestToken}" />""";
    return Results.Content(html, "text/html");
});

app.MapPost("/upload", async (IFormFile file, [FromForm] string name, TravelyRepository repository) =>
{
    if (!IsImage(file))
    {
        return Results.BadRequest("Invalid file type");
    }

    using var image = await Image.LoadAsync(file.OpenReadStream());
    image.Mutate(x => x.Resize(300, 300));
    
    await using var memoryStream = new MemoryStream();
    image.Save(memoryStream, new WebpEncoder { Quality = 75});
    
    var category = new CategoriesModel
    {
        Id = Guid.NewGuid().GetHashCode(),
        Name = name,
        Image = memoryStream.ToArray()
    };

    await repository.AddCategory(category);

    return Results.Ok();
});

app.MapGet("/categories", async (TravelyRepository repository) =>
{
    var categories = await repository.GetCategoriesInfo();
    var html = categories.Aggregate("<div class=\"owl-carousel mt-4\">",
        (current, category) =>
            current +
            $"""
             <div class="item">
                 <div class="rounded-top-circle overflow-hidden ms-4 shadow"
                      style="max-width: 80%">
                     <img src="/image/{category.Id}" height="300" loading="lazy" alt="{category.Name}"/>
                 </div>
                 <h2 class="text-center mt-2">{category.Name}</h2>
             </div>
             """
    );
    html += "</div>";
    // i want to return string to be updated buy unpoly
    return Results.Content(html, "text/html");
});

app.MapGet("/categoriesList", async (TravelyRepository repository) =>
{
    var categories = await repository.GetCategoriesInfo();
    var html = categories.Aggregate("",
        (current, category) =>
            current +
            $"""<li><a class="dropdown-item" hx-delete="/delete/{category.Id}" hx-swap="outerHTML">{category.Name}</a></li>"""
    );
    return Results.Content(html, "text/html");
});

app.MapGet("/image/{id:int}", async (int id, TravelyRepository repository) =>
{
    var image = await repository.GetImage(id);
    return Results.File(image, "image/webp");
});

app.MapDelete("/delete/{id:int}", async (int id, TravelyRepository repository) =>
{
    await repository.DeleteCategory(id);
    return Results.Ok();
});

app.Run();
return;

bool IsImage(IFormFile file)
{
    return file.ContentType.StartsWith("image");
}

internal class CategoriesModel
{
    public int? Id { get; init; }
    public string? Name { get; init; }
    public byte[]? Image { get; init; }
}

internal class TravelyRepository(string connectionString)
{
    public async Task CreateDatabaseTable()
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.ExecuteAsync(
            """
                CREATE TABLE IF NOT EXISTS Categories (
                Id INTEGER PRIMARY KEY,
                Name TEXT NOT NULL,
                Image BLOB NOT NULL
                )
            """);
    }

    public async Task<IEnumerable<CategoriesModel>> GetCategoriesInfo()
    {
        await using var connection = new SqliteConnection(connectionString);
        return await connection.QueryAsync<CategoriesModel>("SELECT Id, name FROM Categories");
    }

    public async Task<byte[]> GetImage(int id)
    {
        await using var connection = new SqliteConnection(connectionString);
        return await connection.QuerySingleAsync<byte[]>("SELECT Image FROM Categories WHERE Id = @id", new { id });
    }

    public async Task AddCategory(CategoriesModel category)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.ExecuteAsync("INSERT INTO Categories (Id, Name, Image) VALUES (@Id, @Name, @Image)", category);
    }

    public async Task DeleteCategory(int id)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.ExecuteAsync("DELETE FROM Categories WHERE Id = @id", new { id });
    }
}