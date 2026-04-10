using APME.AI;
using APME.AI.QueryUnderstanding;
using APME.Chat;
using APME.Dashboard;
using APME.Payments;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Account;
using Volo.Abp.AutoMapper;
using Volo.Abp.BlobStoring;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.PermissionManagement;
using Volo.Abp.SettingManagement;
using Volo.Abp.TenantManagement;

namespace APME;

[DependsOn(
    typeof(APMEDomainModule),
    typeof(AbpAccountApplicationModule),
    typeof(APMEApplicationContractsModule),
    typeof(AbpIdentityApplicationModule),
    typeof(AbpPermissionManagementApplicationModule),
    typeof(AbpTenantManagementApplicationModule),
    typeof(AbpFeatureManagementApplicationModule),
    typeof(AbpSettingManagementApplicationModule),
    typeof(AbpBlobStoringModule)
    )]
public class APMEApplicationModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();

        Configure<AbpAutoMapperOptions>(options =>
        {
            options.AddMaps<APMEApplicationModule>();
        });
        
        Configure<AbpMultiTenancyOptions>(options =>
        {
            options.IsEnabled = true;
        });

        // Configure Stripe options
        context.Services.Configure<StripeOptions>(
            configuration.GetSection(StripeOptions.SectionName));

        // Register payment service
        context.Services.AddTransient<IPaymentService, StripePaymentService>();

        // Configure AI options
        context.Services.Configure<AIOptions>(
            configuration.GetSection(AIOptions.SectionName));

        // Configure Chat options
        context.Services.Configure<ChatOptions>(
            configuration.GetSection(ChatOptions.SectionName));

        // Configure Query Understanding options
        context.Services.Configure<QueryUnderstandingOptions>(
            configuration.GetSection(QueryUnderstandingOptions.SectionName));

        // Register AI Services - Layer isolation with anti-corruption layer
        context.Services.AddTransient<ILlmProvider, OllamaProvider>();

        // Register Query Understanding Services - Phase 3 RAG Optimization
        context.Services.AddTransient<IIntentClassifier, HybridIntentClassifier>();
        context.Services.AddTransient<IEntityRecognizer, HybridEntityRecognizer>();
        context.Services.AddTransient<IQueryExpander, QueryExpander>();
        context.Services.AddTransient<IQueryRewriter, QueryRewriter>();
        context.Services.AddTransient<AI.QueryUnderstanding.IQueryUnderstandingService, AI.QueryUnderstanding.QueryUnderstandingService>();

        // Register Chat Services - Context management and fallback strategies
        context.Services.AddTransient<IChatContextService, ChatContextService>();
        context.Services.AddTransient<IFallbackStrategyProvider, FallbackStrategyProvider>();

        // Register Dashboard Service
        context.Services.AddTransient<IDashboardAppService, DashboardAppService>();
    }
}
