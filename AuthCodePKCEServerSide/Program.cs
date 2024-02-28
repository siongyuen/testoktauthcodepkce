
using AuthCodePKCEServerSide;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<AuthCodePKCEServerSide.ICustomTokenValidator, AuthCodePKCEServerSide.CustomTokenValidator>();
builder.Services.Configure <IdpSettings >(builder.Configuration.GetSection("Idp"));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.SaveToken = true;
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = async context =>
                {

                    var tokenValidator = context.HttpContext.RequestServices.GetRequiredService<AuthCodePKCEServerSide.ICustomTokenValidator>();
                    var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                    context.Token = token;
                    var oktaDomain = "https://dev-95411323.okta.com";
                    var oktaConfig = new OktaConfiguration(oktaDomain);

                    var isValidToken = token != null && await tokenValidator.ValidateToken(token, oktaConfig);

                    if (!isValidToken)
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
