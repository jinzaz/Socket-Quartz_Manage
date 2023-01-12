using Quartz;
using Quartz.Spi;
using System;

namespace Socket_Quartz_Manage.Quartz
{
    public class DefaultJobFactory : IJobFactory
    {
        public readonly IServiceProvider _serviceProvider;
        public DefaultJobFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
        {
            var jobType = bundle.JobDetail.JobType;
            return _serviceProvider.GetService(jobType) as IJob;
        }

        public void ReturnJob(IJob job)
        {
            var disposable = job as IDisposable;
            disposable?.Dispose();
        }
    }
}
