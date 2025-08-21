


using Backend;
using Backend.asp.Services.Interfaces;
using HttpServer.asp.Services;
using HttpServer.asp.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;




var builder = WebApplication.CreateBuilder(args);





// Optionally, add services to the container here
builder.Services.AddScoped<IUserManager, UserManager>();
builder.Services.AddScoped<IStorageProvider, LocalStorageProvider>();
// For example, if you plan to use controllers:
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(op =>
                {
                    op.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,

                        ValidIssuer = "MusicLabBackEndApi",
                        ValidAudience = "MusicLabAppUsers",
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("YourSuperSecretKey_ChangeThislater123456789"))

                    };
                });



builder.Services.AddAuthorization();

builder.Services.AddCors();




// builder.Services.AddMinio( configureClient =>
//     configureClient.WithEndpoint(endpoint)
//                     .WithCredentials(accessKey, secretKey).
//                     Build()
// );



builder.Services.AddSingleton<IJwtService, JwtService>();
builder.Services.AddTransient<IWaveformGeneratorService, WaveformGeneratorService>();
// Read the secret key from configuration
var signedUrlConfig = builder.Configuration.GetSection("SignedUrl");
var secretKey = signedUrlConfig.GetValue<string>("SecretKey");

builder.Services.AddSingleton(new SignedUrlService(secretKey));

// 🔹 Database setup depending on environment
//if (builder.Environment.IsDevelopment())
//{
//    // SQL Server in dev
//    builder.Services.AddDbContext<AppDbContext>(options =>
//        options.UseSqlServer(@"Server=.\SQLEXPRESS;Database=FileUploadDEMO;
//                                Trusted_Connection=True;TrustServerCertificate=True;
//                                Integrated Security=True"));
//}
//else
//{
// SQLite in production

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));



//}





builder.Services.AddControllers();


var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();


    db.Database.Migrate(); // applies only if there are pending migrations

}



app.UseCors(builder =>
            {
                builder.AllowAnyOrigin()  // Allow access from any IP
                       .AllowAnyMethod()
                       .AllowAnyHeader();
            });

app.UseAuthentication();
app.UseAuthorization();



// Configure the HTTP request pipeline.
// If you added controllers, map them:
app.MapControllers();



// Alternatively, for a simple endpoint, you can add:
app.MapGet("/", () => "Hello from the empty ASP.NET Core HTTP Server!");


app.Run();

