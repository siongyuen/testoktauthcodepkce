
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
                    var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                    context.Token = token;                                   

                    var isValidToken = token != null && await tokenHelper.ValidateToken(token, idpSettings);
                    if (isValidToken)
                    {
                       var claims = tokenHelper.ExtractClaim(token).Result ;
                       var identity = new ClaimsIdentity(claims, JwtBearerDefaults.AuthenticationScheme);
                       var principal = new ClaimsPrincipal(identity);
                       context.Principal = principal;
                       context.Success(); // Marks the message as successfully processed, no need for further authentication
                    }
                    else
                    {
                        context.Fail("Invalid token"); // Explicitly fail authentication if token is invalid
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
