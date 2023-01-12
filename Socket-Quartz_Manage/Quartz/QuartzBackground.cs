using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Quartz;
using Quartz.Spi;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Socket_Quartz_Manage.Quartz
{
    public class QuartzBackground : BackgroundService, IQuartzBackground
    {

        private readonly ISchedulerFactory _schedulerFactory;
        private readonly IJobFactory _jobFactory;
        private readonly List<Type> _jobTypes;

        public QuartzBackground(
            ISchedulerFactory schedulerFactory,
            IJobFactory jobFactory)
        {
            _schedulerFactory = schedulerFactory;
            _jobFactory = jobFactory;

            _jobTypes = Assembly.Load(Assembly.GetEntryAssembly().FullName)
                .GetTypes()
                .Where(t => t.Name.EndsWith("Job"))
                .ToList();
        }
        public IScheduler Scheduler { get; set; }

        /// <summary>
        /// 启动任务调度
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            Scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
            Scheduler.JobFactory = _jobFactory;

            List<dynamic> jobs_list = new List<dynamic>()
            {
                new { ip_address="188.128.0.244",job_type="DefaultJob",Interval=3 },
                new { ip_address="188.128.0.244",job_type="TestJob",Interval=5 },
            };

            foreach (var item in jobs_list)
            {
                var ip = item.ip_address;
                var job_type = item.job_type;
                var IntervalSecond = item.Interval;

                JobDataMap map = new JobDataMap()
                {
                    new KeyValuePair<string, object>("IPAddress",ip), //携带参数1
                };
                string CornExpress = $"*/{IntervalSecond} * * * * ?";
                JobSchedule jobSchedule = new JobSchedule(job_type, CornExpress, map);
                var job = CreateJob(jobSchedule);
                var trigger = CreateTrigger(jobSchedule);

                await Scheduler.ScheduleJob(job, trigger, cancellationToken);
                

            }
            await Scheduler.Start(cancellationToken);
        }

        /// <summary>
        /// 停止调度
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Scheduler?.Shutdown(cancellationToken);
        }

        /// <summary>
        /// 添加任务到调度器
        /// </summary>
        /// <param name="schedule"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task AddJobForScheduler(JobSchedule schedule, CancellationToken cancellationToken = default)
        {
            var job = CreateJob(schedule);
            var trigger = CreateTrigger(schedule);

            await Scheduler.ScheduleJob(job, trigger, cancellationToken);
        }

        /// <summary>
        /// 从调度器删除任务
        /// </summary>
        /// <param name="schedule"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task DeleteJobForScheduler(JobSchedule schedule, CancellationToken cancellationToken = default)
        {

            var jobType = _jobTypes.Find(x => x.Name == schedule.JobType);
            var sn = schedule.jobDataMap.GetString("IPAddress");
            JobKey jobKey = new JobKey(sn, jobType.FullName);
            await Scheduler.DeleteJob(jobKey);
        }


        /// <summary>
        /// 创建任务体
        /// </summary>
        /// <param name="schedule"></param>
        /// <returns></returns>
        private IJobDetail CreateJob(JobSchedule schedule)
        {
            var jobType = _jobTypes.Find(x => x.Name == schedule.JobType);
            var sn = schedule.jobDataMap.GetString("IPAddress");
            return JobBuilder
                .Create(jobType)
                .SetJobData(schedule.jobDataMap)
                .WithIdentity(sn, jobType.FullName)
                .WithDescription(jobType.Name)
                .Build();
        }

        /// <summary>
        /// 创建触发器
        /// </summary>
        /// <param name="schedule"></param>
        /// <returns></returns>
        private ITrigger CreateTrigger(JobSchedule schedule)
        {
            var jobType = _jobTypes.Find(x => x.Name == schedule.JobType);
            var sn = schedule.jobDataMap.GetString("IPAddress");
            return TriggerBuilder
                .Create()
                .WithIdentity(sn, $"{jobType.FullName}.trigger")
                .WithCronSchedule(schedule.CronExpression)
                .WithDescription(schedule.CronExpression)
                .Build();
        }
    }
}
