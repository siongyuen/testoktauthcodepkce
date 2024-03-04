
using AuthCodePKCEServerSide;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<AuthCodePKCEServerSide.ICustomTokenHelper, AuthCodePKCEServerSide.CustomTokenHelper>();
builder.Services.Configure <IdpSettings >(builder.Configuration.GetSection("Idp"));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.SaveToken = true;
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = async context =>
                {
                    var tokenHelper = context.HttpContext.RequestServices.GetRequiredService<AuthCodePKCEServerSide.ICustomTokenHelper>();
                    var idpSettings = context.HttpContext.RequestServices.GetRequiredService<IOptions<IdpSettings>>().Value;


                    var authorizationHeader = context.Request.Headers["Authorization"].FirstOrDefault();
                    var token = authorizationHeader?.Split(" ").Last();


                    if (string.IsNullOrEmpty(token))
                    {
                        context.Fail("Token not provided");
                        return;
                    }


                    var isValidToken = await tokenHelper.ValidateToken(token, idpSettings);
                    if (isValidToken)
                    {
                        var claims = await tokenHelper.ExtractClaim(token);
                        var identity = new ClaimsIdentity(claims, JwtBearerDefaults.AuthenticationScheme);
                        var principal = new ClaimsPrincipal(identity);

                        context.Principal = principal;
                        context.Success();
                    }
                    else
                    {
                        context.Fail("Invalid token");
                    }
                }        
            };
        });


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
