


using Backend.asp.Services.Interfaces;

using Microsoft.EntityFrameworkCore;

namespace Backend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);


            



            // Optionally, add services to the container here
            // For example, if you plan to use controllers:
            builder.Services.AddCors();
            builder.Services.AddScoped<IUserManager, UserManager>();

            builder.Services.AddSingleton<IJwtService, JwtService>();

            builder.Services.AddDbContext<AppDbContext>(options =>
                    options.UseSqlServer(@"Server=.\SQLEXPRESS;Database=FileUploadDEMO;
                                                Trusted_Connection=True;TrustServerCertificate=True;
                                                    Integrated Security=True"));
            builder.Services.AddControllers();


            var app = builder.Build();

            app.UseCors(builder =>
            {
                builder.AllowAnyOrigin()  // Allow access from any IP
                       .AllowAnyMethod()
                       .AllowAnyHeader();
            });

            



            // Configure the HTTP request pipeline.
            // If you added controllers, map them:
            app.MapControllers();



            // Alternatively, for a simple endpoint, you can add:
            app.MapGet("/", () => "Hello from the empty ASP.NET Core HTTP Server!");



            app.Run();
        }
    }
}
