using System.Linq;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using APME.Customers;
using APME.MultiTenancy;
using Volo.Abp.AuditLogging;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.Emailing;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Identity;
using Volo.Abp.Localization;
using Volo.Abp.Modularity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.OpenIddict;
using Volo.Abp.PermissionManagement.Identity;
using Volo.Abp.PermissionManagement.OpenIddict;
using Volo.Abp.Security.Claims;
using Volo.Abp.SettingManagement;
using Volo.Abp.TenantManagement;

namespace APME;

[DependsOn(
    typeof(APMEDomainSharedModule),
    typeof(AbpAuditLoggingDomainModule),
    typeof(AbpBackgroundJobsDomainModule),
    typeof(AbpFeatureManagementDomainModule),
    typeof(AbpIdentityDomainModule),
    typeof(AbpOpenIddictDomainModule),
    typeof(AbpPermissionManagementDomainOpenIddictModule),
    typeof(AbpPermissionManagementDomainIdentityModule),
    typeof(AbpSettingManagementDomainModule),
    typeof(AbpTenantManagementDomainModule),
    typeof(AbpEmailingModule)
)]
public class APMEDomainModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpLocalizationOptions>(options =>
        {
            options.Languages.Add(new LanguageInfo("ar", "ar", "العربية"));
            options.Languages.Add(new LanguageInfo("cs", "cs", "Čeština"));
            options.Languages.Add(new LanguageInfo("en", "en", "English"));
            options.Languages.Add(new LanguageInfo("en-GB", "en-GB", "English (UK)"));
            options.Languages.Add(new LanguageInfo("hu", "hu", "Magyar"));
            options.Languages.Add(new LanguageInfo("hr", "hr", "Croatian"));
            options.Languages.Add(new LanguageInfo("fi", "fi", "Finnish"));
            options.Languages.Add(new LanguageInfo("fr", "fr", "Français"));
            options.Languages.Add(new LanguageInfo("hi", "hi", "Hindi"));
            options.Languages.Add(new LanguageInfo("it", "it", "Italiano"));
            options.Languages.Add(new LanguageInfo("pt-BR", "pt-BR", "Português"));
            options.Languages.Add(new LanguageInfo("ru", "ru", "Русский"));
            options.Languages.Add(new LanguageInfo("sk", "sk", "Slovak"));
            options.Languages.Add(new LanguageInfo("tr", "tr", "Türkçe"));
            options.Languages.Add(new LanguageInfo("zh-Hans", "zh-Hans", "简体中文"));
            options.Languages.Add(new LanguageInfo("zh-Hant", "zh-Hant", "繁體中文"));
            options.Languages.Add(new LanguageInfo("de-DE", "de-DE", "Deutsch"));
            options.Languages.Add(new LanguageInfo("es", "es", "Español"));
        });

        Configure<AbpMultiTenancyOptions>(options =>
        {
            options.IsEnabled = MultiTenancyConsts.IsEnabled;
        });

        // Configure Customer Identity Core (separate from IdentityUser)
        context.Services.AddIdentityCore<Customer>()
            .AddRoles<IdentityRole>()
            .AddClaimsPrincipalFactory<CustomerClaimsPrincipalFactory>();
        context.Services.TryAddScoped<CustomerStore>();
        context.Services.TryAddScoped(typeof(IUserStore<Customer>), provider => provider.GetService(typeof(CustomerStore)));
        context.Services.TryAddScoped<CustomerUserManager>();
        context.Services.TryAddScoped(typeof(UserManager<Customer>), provider => provider.GetService<CustomerUserManager>());

        Configure<IdentityOptions>(options =>
        {
            // Allow non-unique email for customers (they may use phone numbers primarily)
            options.User.RequireUniqueEmail = false;
        });

#if DEBUG
        context.Services.Replace(ServiceDescriptor.Singleton<IEmailSender, NullEmailSender>());
#endif
    }

    public override void PostConfigureServices(ServiceConfigurationContext context)
    {
        // Replace IdentityDynamicClaimsPrincipalContributor with Customer-aware version
        // This must happen after all modules have registered their services
        // This prevents exceptions when Customer GUIDs are looked up in IdentityUser table
        var descriptorsToRemove = context.Services
            .Where(d => d.ServiceType == typeof(IAbpClaimsPrincipalContributor) &&
                       d.ImplementationType?.Name == "IdentityDynamicClaimsPrincipalContributor")
            .ToList();
        
        foreach (var descriptor in descriptorsToRemove)
        {
            context.Services.Remove(descriptor);
        }
        
        context.Services.AddTransient<IAbpClaimsPrincipalContributor, CustomerAwareIdentityDynamicClaimsPrincipalContributor>();
    }
}
