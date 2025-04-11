
using Container_Testing.Models.Context;
using Microsoft.EntityFrameworkCore;

namespace Container_Testing
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddDbContext<ETicketingContext>(
                options => options.UseSqlServer(builder.Configuration.GetConnectionString("ETicketingServiceConn"))
            );

            builder.Services.AddDistributedMemoryCache();

            builder.Services.AddSession(options =>
            {
                // dont forget to change this!
                options.IdleTimeout = TimeSpan.FromMinutes(10); // Extend session lifetime
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.None; // Needed for cross-origin requests
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Ensure HTTPS cookies
                options.Cookie.IsEssential = true;
            });

            builder.Services.AddCors(options =>
            {
                options.AddPolicy(name: MyAllowSpecificOrigins,
                    policy =>
                    {
                        policy.WithOrigins("http://localhost:3039") // Allow frontend origin
                              .AllowCredentials()
                              .AllowAnyMethod()
                              .AllowAnyHeader();
                    });
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseCors(MyAllowSpecificOrigins);

            app.UseSession();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
