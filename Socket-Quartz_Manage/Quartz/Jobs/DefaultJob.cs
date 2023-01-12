using Quartz;

namespace Socket_Quartz_Manage.Quartz.Jobs
{
    public class DefaultJob : IJob
    {
        public DefaultJob() 
        {

        }
        public Task Execute(IJobExecutionContext context)
        {
            throw new NotImplementedException();
        }
    }
}
