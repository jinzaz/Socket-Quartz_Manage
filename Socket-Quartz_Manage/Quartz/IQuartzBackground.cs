using System.Threading;
using System.Threading.Tasks;

namespace Socket_Quartz_Manage.Quartz
{
    public interface IQuartzBackground
    {
        Task AddJobForScheduler(JobSchedule schedule, CancellationToken cancellationToken = default);
        Task DeleteJobForScheduler(JobSchedule schedule, CancellationToken cancellationToken = default);
    }
}
