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

var builder = WebApplication.CreateBuilder(args);

// ----------------------------------------------------
// 1. Configuration and Connection String Retrieval
// ----------------------------------------------------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
    throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// ----------------------------------------------------
// 2. Add services to the container (Core ASP.NET Identity/MVC)
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
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultUI() // Keep this here to register the Identity Pages
.AddDefaultTokenProviders();

//builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
//{
//    options.SignIn.RequireConfirmedAccount = true;
//})
//.AddEntityFrameworkStores<ApplicationDbContext>()
//.AddDefaultUI() // Keep this here to register the Identity Pages
//.AddDefaultTokenProviders();

// c. MVC/Controllers
builder.Services.AddControllersWithViews();


// ----------------------------------------------------
// 3. AWS Service Registration
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
builder.Services.AddScoped<ICommentRepository, CommentRepository>();
builder.Services.AddScoped<IPodcastRepository, PodcastRepository>();
builder.Services.AddScoped<IS3Service, S3Service>();             
builder.Services.AddScoped<IParameterStoreService, ParameterStoreService>(); 
// ----------------------------------------------------


var app = builder.Build();

// Adding roles 
//using (var scope = app.Services.CreateScope())
//{
//    var serviceProvider = scope.ServiceProvider;
//    var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

//    // Required roles: Podcaster, Listener/viewer, Admin
//    string[] roleNames = { "Admin", "Podcaster", "Listener/viewer" };

//    foreach (var roleName in roleNames)
//    {
//        // Check if the role already exists asynchronously
//        if (!roleManager.RoleExistsAsync(roleName).GetAwaiter().GetResult())
//        {
//            // If the role doesn't exist, create it
//            roleManager.CreateAsync(new IdentityRole(roleName)).GetAwaiter().GetResult();
//        }
//    }
//}

using (var scope = app.Services.CreateScope())
{
    //seeds User Roles onto db on every run of app (To ensure db has Roles)
    await SeedRolesCreatedAsync(scope.ServiceProvider);
    //seeds Users onto db on every run of app (To ensure db has Users)
    await SeedTestListenerViewerUserAsync(scope.ServiceProvider);
    await SeedTestPodcasterUserAsync(scope.ServiceProvider);
    await SeedTestAdminUserAsync(scope.ServiceProvider);
}


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












//static async Task CreateRolesAsync(WebApplication app)
//{
//    using var scope = app.Services.CreateScope();
//    var serviceProvider = scope.ServiceProvider;
//    var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

//    string[] roleNames = { "Admin", "Podcaster", "Listener/viewer" };

//    foreach (var roleName in roleNames)
//    {
//        if (!await roleManager.RoleExistsAsync(roleName))
//        {
//            await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
//        }
//    }
//}

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

