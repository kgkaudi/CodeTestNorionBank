using CodeSolution.Core.Fees;
using CodeSolution.Core.Holidays;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddSingleton<IPublicHolidayProvider, SwedishPublicHolidayProvider>();
builder.Services.AddSingleton<TollFeeSchedule>();
builder.Services.AddSingleton<ITollCalculator, TollCalculator>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();

public partial class Program { }
