using APME.Samples;
using Xunit;

namespace APME.EntityFrameworkCore.Domains;

[Collection(APMETestConsts.CollectionDefinitionName)]
public class EfCoreSampleDomainTests : SampleDomainTests<APMEEntityFrameworkCoreTestModule>
{

}
