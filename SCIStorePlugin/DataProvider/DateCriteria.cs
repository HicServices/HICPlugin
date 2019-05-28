using System;

namespace SCIStorePlugin.DataProvider
{
    public class DateCriteria
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        public void EachDay(Action<DateTime, DateTime> action)
        {
            var currDate = FromDate;
            var oneDay = new TimeSpan(1, 0, 0, 0);
            while (currDate <= ToDate)
            {
                var nextDate = currDate.Add(oneDay);
                if (nextDate > DateTime.Now)
                    nextDate = DateTime.Now;

                if (nextDate > ToDate)
                {
                    if (currDate < ToDate)
                        action(currDate, ToDate);
                    break;
                }

                action(currDate, nextDate);

                currDate = nextDate;
            }
        }
    }
}