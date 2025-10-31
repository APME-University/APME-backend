using APME.Samples;
using Xunit;

namespace APME.EntityFrameworkCore.Applications;

[Collection(APMETestConsts.CollectionDefinitionName)]
public class EfCoreSampleAppServiceTests : SampleAppServiceTests<APMEEntityFrameworkCoreTestModule>
{

}
