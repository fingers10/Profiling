using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackExchange.Profiling;
using StackExchange.Profiling.Storage;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<ProfilingContext>();

//var storage = new PostgreSqlStorage("Host=localhost;Database=database;User id=postgres;Password=password;");
//foreach (var cs in storage.TableCreationScripts)
//{
//    Console.WriteLine(cs);
//}

builder.Services.AddMiniProfiler(options =>
{
    options.RouteBasePath = "/profiler"; // /profiler/results-index
    options.IgnoredPaths.Add("/swagger");
    options.Storage = new PostgreSqlStorage("Host=localhost;Database=database;User id=postgres;Password=password;");
    options.ResultsAuthorize = request => IsUserAuthorized(request);
    options.ResultsListAuthorize = request => IsUserAuthorized(request);
    options.ShouldProfile = request => ShouldProfile(request);
    options.ColorScheme = ColorScheme.Dark;
}).AddEntityFramework();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMiniProfiler();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/authors", async ([FromServices] ProfilingContext context) =>
{
    var authors = await context.Authors.ToListAsync();

    using (MiniProfiler.Current.Step("Returning data"))
    { 
        return authors;
    }
})
.WithName("GetAuthors");

app.Run();

static bool IsUserAuthorized(HttpRequest request)
{
    return request.HttpContext.User.Identity!.IsAuthenticated;
}

static bool ShouldProfile(HttpRequest request)
{
    // only profile api's
    return request.Path.StartsWithSegments("/api");
}

public class Author
{
    public long Id { get; set; }
    public string Name { get; set; } = default!;
}

public class ProfilingContext : DbContext
{
    public DbSet<Author> Authors => Set<Author>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql("Host=localhost;Database=database;User id=postgres;Password=password;");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Author>().ToTable("Authors");
        modelBuilder.Entity<Author>().HasKey(a => a.Id);
    }
}