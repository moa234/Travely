using Dapper;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("TravelyConnection");
builder.Services.AddAntiforgery();
builder.Services.AddSingleton(new TravelyRepository(connectionString ?? throw new InvalidOperationException()));


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

    await using var memoryStream = new MemoryStream();
    await file.CopyToAsync(memoryStream);

    var category = new CategoriesModel
    {
        Id = Guid.NewGuid().GetHashCode(),
        Name = name,
        Image = memoryStream.ToArray()
    };

    repository.AddCategory(category);

    return Results.Ok();
});

app.MapGet("/categories", (TravelyRepository repository) =>
{
    var categories = repository.GetCategoriesInfo();
    var html = categories.Aggregate("<div class=\"owl-carousel mt-4\">",
        (current, category) =>
            current +
            $"""
             <div class="item">
                 <div class="rounded-top-circle overflow-hidden ms-4 shadow"
                      style="max-width: 80%">
                     <img src="/image/{category.Id}" height="300" loading="lazy"/>
                 </div>
                 <h2 class="text-center mt-2">{category.Name}</h2>
             </div>
             """
    );
    html += "</div>";
    // i want to return string to be updated buy unpoly
    return Results.Content(html, "text/html");
});

app.MapGet("/categoriesList", (TravelyRepository repository) =>
{
    var categories = repository.GetCategoriesInfo();
    var html = categories.Aggregate("",
        (current, category) =>
            current +
            $"""<li><a class="dropdown-item" hx-delete="/delete/{category.Id}">{category.Name}</a></li>"""
    );
    return Results.Content(html, "text/html");
});

app.MapGet("/image/{id:int}", (int id, TravelyRepository repository) =>
{
    var image = repository.GetImage(id);
    return Results.File(image, "image/png");
});

app.MapDelete("/delete/{id:int}", (int id, TravelyRepository repository) =>
{
    repository.DeleteCategory(id);
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

internal class TravelyRepository
{
    private readonly string _connectionString;

    public TravelyRepository(string connectionString)
    {
        _connectionString = connectionString;
        CreateDatabaseTable();
    }

    private void CreateDatabaseTable()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Execute(
            """
                CREATE TABLE IF NOT EXISTS Categories (
                Id INTEGER PRIMARY KEY,
                Name TEXT NOT NULL,
                Image BLOB NOT NULL
                )
            """);
    }

    public IEnumerable<CategoriesModel> GetCategoriesInfo()
    {
        using var connection = new SqliteConnection(_connectionString);
        return connection.Query<CategoriesModel>("SELECT Id, name FROM Categories");
    }

    public byte[] GetImage(int id)
    {
        using var connection = new SqliteConnection(_connectionString);
        return connection.QuerySingle<byte[]>("SELECT Image FROM Categories WHERE Id = @id", new { id });
    }

    public void AddCategory(CategoriesModel category)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Execute("INSERT INTO Categories (Id, Name, Image) VALUES (@Id, @Name, @Image)", category);
    }
    
    public void DeleteCategory(int id)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Execute("DELETE FROM Categories WHERE Id = @id", new { id });
    }
}