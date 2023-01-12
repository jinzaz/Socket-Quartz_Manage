using Quartz;

namespace Socket_Quartz_Manage.Quartz
{
    public class JobSchedule
    {
        public JobSchedule(string jobType, string cronExpression, JobDataMap dataMap)
        {
            JobType = jobType;
            CronExpression = cronExpression;
            jobDataMap = dataMap;
        }

        public string JobType { get; }
        public string CronExpression { get; }
        public JobDataMap jobDataMap { get; set; }
    }
}
