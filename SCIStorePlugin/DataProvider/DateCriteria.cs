using System;

namespace SCIStorePlugin.DataProvider;

public class DateCriteria
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }

    public void EachDay(Action<DateTime, DateTime> action)
    {
        var currentDate = FromDate;
        var oneDay = new TimeSpan(1, 0, 0, 0);
        while (currentDate <= ToDate)
        {
            var nextDate = currentDate.Add(oneDay);
            if (nextDate > DateTime.Now)
                nextDate = DateTime.Now;

            if (nextDate > ToDate)
            {
                if (currentDate < ToDate)
                    action(currentDate, ToDate);
                break;
            }

            action(currentDate, nextDate);

            currentDate = nextDate;
        }
    }
}