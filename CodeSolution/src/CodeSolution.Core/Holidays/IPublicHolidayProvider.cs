namespace CodeSolution.Core.Holidays;

public interface IPublicHolidayProvider
{
    bool IsPublicHoliday(DateOnly date);
}
