namespace APME.Permissions;

public static class APMEPermissions
{
    public const string GroupName = "APME";

    public static class Costing
    {
        public const string Default = GroupName + ".Costing";
        public const string View = Default + ".View";
        public const string ManageCost = Default + ".ManageCost";
        public const string ManagePolicy = Default + ".ManagePolicy";
    }

    public static class PricingAdvisor
    {
        public const string Default = GroupName + ".PricingAdvisor";
        public const string View = Default + ".View";
        public const string Generate = Default + ".Generate";
        public const string Approve = Default + ".Approve";
        public const string ManagePolicy = Default + ".ManagePolicy";
        public const string ManageCompetitor = Default + ".ManageCompetitor";
    }
}
