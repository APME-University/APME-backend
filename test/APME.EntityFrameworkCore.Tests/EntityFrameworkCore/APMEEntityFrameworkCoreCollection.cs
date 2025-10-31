using Xunit;

namespace APME.EntityFrameworkCore;

[CollectionDefinition(APMETestConsts.CollectionDefinitionName)]
public class APMEEntityFrameworkCoreCollection : ICollectionFixture<APMEEntityFrameworkCoreFixture>
{

}
