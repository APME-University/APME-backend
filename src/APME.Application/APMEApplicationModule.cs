using System;
using Microsoft.Extensions.Configuration;
using APME.AI;
using APME.AI.QueryUnderstanding;
using APME.Chat;
using APME.Chatbot;
using APME.Chatbot.Embeddings;
using APME.Chatbot.Handlers;
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
        context.Services.AddTransient<AI.QueryUnderstanding.IIntentClassifier, HybridIntentClassifier>();
        context.Services.AddTransient<IEntityRecognizer, HybridEntityRecognizer>();
        context.Services.AddTransient<IQueryExpander, QueryExpander>();
        context.Services.AddTransient<IQueryRewriter, QueryRewriter>();
        context.Services.AddTransient<AI.QueryUnderstanding.IQueryUnderstandingService, AI.QueryUnderstanding.QueryUnderstandingService>();

        // Register Chat Services - Context management and fallback strategies
        context.Services.AddTransient<IChatContextService, ChatContextService>();
        context.Services.AddTransient<IFallbackStrategyProvider, FallbackStrategyProvider>();

        // Register Dashboard Service
        context.Services.AddTransient<IDashboardAppService, DashboardAppService>();

        // Pricing Advisor demand oracle — select the stub (default) or the FastAPI GRU client.
        // Registered explicitly here (StubDemandClient carries no auto-DI marker) so exactly one
        // IPricingAdvisorClient is ever bound.
        context.Services.Configure<PricingAdvisor.PricingAdvisorOptions>(
            configuration.GetSection(PricingAdvisor.PricingAdvisorOptions.SectionName));
        var advisorOptions = configuration
            .GetSection(PricingAdvisor.PricingAdvisorOptions.SectionName)
            .Get<PricingAdvisor.PricingAdvisorOptions>() ?? new PricingAdvisor.PricingAdvisorOptions();
        if (advisorOptions.UseStub)
        {
            context.Services.AddTransient<PricingAdvisor.IPricingAdvisorClient, PricingAdvisor.StubDemandClient>();
        }
        else
        {
            context.Services.AddHttpClient<PricingAdvisor.IPricingAdvisorClient, PricingAdvisor.HttpPricingAdvisorClient>(client =>
            {
                client.BaseAddress = new Uri(advisorOptions.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(advisorOptions.TimeoutSeconds);
            });
        }

        // Register Chatbot Services — Intent Classification Engine
        context.Services.AddTransient<APME.Chatbot.IIntentClassifier, LlmIntentClassifier>();
        context.Services.AddTransient<ChatAppService>();

        // Register Hybrid Search Services — Phase 3 Embedding Pipeline + Vector Search
        context.Services.AddTransient<IHybridSearchService, HybridSearchService>();
        context.Services.AddTransient<HardFilterStep>();
        context.Services.AddTransient<ScoreFusionRanker>();
        context.Services.AddTransient<EmbeddingAdminAppService>();

        // Register Intent Handlers — ABP conventional DI doesn't expose cross-assembly interfaces
        context.Services.AddTransient<IIntentHandler, FallbackHandler>();
        context.Services.AddTransient<IIntentHandler, OffTopicHandler>();
        context.Services.AddTransient<IIntentHandler, ProductSearchHandler>();
        context.Services.AddTransient<IIntentHandler, ProductComparisonHandler>();
        context.Services.AddTransient<IIntentHandler, ProductDetailsHandler>();
        context.Services.AddTransient<IIntentHandler, RecommendationHandler>();
        context.Services.AddTransient<IIntentHandler, CategoryBrowseHandler>();
        context.Services.AddTransient<IIntentHandler, OrderStatusHandler>();
        context.Services.AddTransient<IIntentHandler, PolicyHandler>();
        context.Services.AddTransient<IIntentHandler, AccountManagementHandler>();
    }
}
