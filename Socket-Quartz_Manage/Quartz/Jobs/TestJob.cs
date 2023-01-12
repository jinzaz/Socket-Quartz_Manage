using Quartz;

namespace Socket_Quartz_Manage.Quartz.Jobs
{
    public class TestJob : IJob
    {
        public TestJob() { }
        public Task Execute(IJobExecutionContext context)
        {
            throw new NotImplementedException();
        }
    }
}
