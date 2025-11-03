using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Extensions.NETCore.Setup;
using Amazon.S3;
using Amazon.SimpleSystemsManagement;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PodcastManagementSystem.Data;
using PodcastManagementSystem.Interfaces;
using PodcastManagementSystem.Models;
using PodcastManagementSystem.Repositories;
using PodcastManagementSystem.Services;
using Amazon.Extensions.Configuration.SystemsManager;
using Amazon;

var builder = WebApplication.CreateBuilder(args);


// ----------------------------------------------------
// 1. AWS Parameter Store Integration 
// ----------------------------------------------------
// Retrieve the root path and region from appsettings.json
var ssmPath = builder.Configuration.GetValue<string>("AWS:SSMConfigRootPath");
var awsRegion = builder.Configuration.GetValue<string>("AWS:Region");

// IMPORTANT: This block must execute BEFORE we retrieve the connection string.
if (!string.IsNullOrEmpty(ssmPath))
{
    try
    {
        // Configure the Parameter Store as a high-priority configuration source.
        builder.Configuration.AddSystemsManager(source =>
        {
            source.Path = ssmPath;

            source.AwsOptions = new AWSOptions
            {
                Region = RegionEndpoint.GetBySystemName(awsRegion)
            };

            source.Optional = false; // Fail fast if the secure config can't be loaded
        });
        Console.WriteLine($"Successfully configured Parameter Store integration for path: {ssmPath}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"WARNING: Failed to load configuration from AWS Parameter Store. Using local connection string. Error: {ex.Message}");
    }
}


// ----------------------------------------------------
// 2. Configuration and Connection String Retrieval (MODIFIED)
// ----------------------------------------------------

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
    throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// ----------------------------------------------------
// 3. Add services to the container (Core ASP.NET Identity/MVC)
// ----------------------------------------------------
// a. DbContext Registration 
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

//No need for defaultIdentity as we have specific roles and therfore set custom identities about 10 lines below
//builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true).AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddDatabaseDeveloperPageExceptionFilter();


// b. Identity Configuration 

builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;// set to false on Purpose by Tomislav. This otherwise we must set up email AND phone verification (WAAAAAY out of scope of this project.)
})
.AddRoles<IdentityRole<Guid>>()
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultUI() // Keep this here to register the Identity Pages
.AddDefaultTokenProviders();

//builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
//{
//	 options.SignIn.RequireConfirmedAccount = true;
//})
//.AddEntityFrameworkStores<ApplicationDbContext>()
//.AddDefaultUI() // Keep this here to register the Identity Pages
//.AddDefaultTokenProviders();

// c. MVC/Controllers
builder.Services.AddControllersWithViews();


// ----------------------------------------------------
// 4. AWS Service Registration
// ----------------------------------------------------
// a. Configure AWS Options 
var awsOptions = builder.Configuration.GetAWSOptions();
builder.Services.AddDefaultAWSOptions(awsOptions);

// b. Register AWS Clients (S3, DynamoDB, Parameter Store)
builder.Services.AddAWSService<IAmazonS3>();
builder.Services.AddAWSService<IAmazonDynamoDB>();
builder.Services.AddAWSService<IAmazonSimpleSystemsManagement>();


// c. Register DynamoDB Context
builder.Services.AddScoped<IDynamoDBContext, DynamoDBContext>();

// d. Register Repositories and Services
builder.Services.AddScoped<ICommentRepository, DynamoDbCommentRepository>();
builder.Services.AddScoped<IPodcastRepository, PodcastRepository>();
builder.Services.AddScoped<IS3Service, S3Service>();
builder.Services.AddScoped<IParameterStoreService, ParameterStoreService>();
builder.Services.AddScoped<IEpisodeRepository, EpisodeRepository>();
builder.Services.AddScoped<IAnalyticsRepository, DynamoDbAnalyticsRepository>();
// ----------------------------------------------------

var app = builder.Build();

await SeedDbWithEntities();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();








async Task SeedDbWithEntities()
{
    using (var scope = app.Services.CreateScope())
    {
        //seeds User Roles onto db on every run of app (To ensure db has Roles)
        await SeedRolesCreatedAsync(scope.ServiceProvider);
        //seeds Users onto db on every run of app (To ensure db has Users)
        //await SeedTestListenerViewerUserAsync(scope.ServiceProvider);
        //await SeedTestPodcasterUserAsync(scope.ServiceProvider);
        //await SeedTestAdminUserAsync(scope.ServiceProvider);
        await SeedUsers(scope.ServiceProvider);
        await SeedPodcasts(scope.ServiceProvider);

    }
}


async Task SeedRolesCreatedAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
    string[] roles = { "Admin", "Podcaster", "ListenerViewer" };
    foreach (var role in roles)
    {

        if (!await roleManager.RoleExistsAsync(role))
        {

            await roleManager.CreateAsync(new IdentityRole<Guid>(role));
        }
    }
}

async Task SeedUsers(IServiceProvider services)
{
    await SeedTestListenerViewerUserAsync(services);
    await SeedTestPodcasterUserAsync(services);
    await SeedTestAdminUserAsync(services);
}

async Task SeedTestListenerViewerUserAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

    var email = "listenerviewer@example.com";
    var user = await userManager.FindByEmailAsync(email);
    if (user == null)
    {
        user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            Role = UserRole.ListenerViewer
        };
        await userManager.CreateAsync(user, "Test123!");
        await userManager.AddToRoleAsync(user, user.Role.ToString());
    }
}

async Task SeedTestPodcasterUserAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

    var email = "podcaster@example.com";
    var user = await userManager.FindByEmailAsync(email);
    if (user == null)
    {
        user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            Role = UserRole.Podcaster
        };
        await userManager.CreateAsync(user, "Test123!");
        await userManager.AddToRoleAsync(user, user.Role.ToString());
    }
}

async Task SeedTestAdminUserAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

    var email = "admin@example.com";
    var user = await userManager.FindByEmailAsync(email);
    if (user == null)
    {
        user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            Role = UserRole.Admin
        };
        await userManager.CreateAsync(user, "Test123!");
        await userManager.AddToRoleAsync(user, user.Role.ToString());
    }
}

async Task SeedPodcasts(IServiceProvider services)
{

    using var scope = services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

    // If the DB already has podcasts, skip seeding
    if (dbContext.Podcasts.Any())
    {
        return;
    }

    var podcaster = await userManager.FindByEmailAsync("podcaster@example.com");

    var podcasts = new List<Podcast>
        {
            new Podcast
            {
                Title = "Tech Talks Daily",
                Description = "Your daily dose of tech insights.",
                CreatorID = podcaster.Id,
                //CreatorID = Guid.NewGuid(),
                CreatedDate = DateTime.UtcNow
            },
            new Podcast
            {
                Title = "History Revisited",
                Description = "Exploring untold stories from the past.",
                CreatorID = podcaster.Id,
                //CreatorID = Guid.NewGuid(),
                CreatedDate = DateTime.UtcNow
            },
            new Podcast
            {
                Title = "Mindful Moments",
                Description = "Meditation and mindfulness tips for everyday life.",
                CreatorID = podcaster.Id,
                //CreatorID = Guid.NewGuid(),
                CreatedDate = DateTime.UtcNow
            }
        };

    dbContext.Podcasts.AddRange(podcasts);
    dbContext.SaveChanges();
}
