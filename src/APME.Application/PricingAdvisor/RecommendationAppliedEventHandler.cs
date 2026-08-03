using System;
using System.Threading.Tasks;
using Hangfire;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace APME.PricingAdvisor;

/// <summary>
/// When a recommendation is applied, schedule outcome tracking after the measurement window
/// (30 days) so we can compare predicted vs. actual — the closed-loop learning signal.
/// </summary>
public class RecommendationAppliedEventHandler
    : IDistributedEventHandler<RecommendationAppliedEto>, ITransientDependency
{
    public Task HandleEventAsync(RecommendationAppliedEto eventData)
    {
        BackgroundJob.Schedule<OutcomeTrackingWorker>(
            w => w.MeasureAsync(eventData.TenantId, eventData.RecommendationId),
            TimeSpan.FromDays(30));

        return Task.CompletedTask;
    }
}
