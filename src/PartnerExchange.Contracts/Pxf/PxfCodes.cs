namespace PartnerExchange.Contracts.Pxf;

internal static class PxfCodes
{
    public static class Status
    {
        public static string ToV1(PxfStatus status) => status switch
        {
            PxfStatus.Operational => "1",
            PxfStatus.Limited => "2",
            PxfStatus.NonOperational => "3",
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
        };

        public static string ToV2(PxfStatus status) => status switch
        {
            PxfStatus.Operational => "OPR",
            PxfStatus.Limited => "LIM",
            PxfStatus.NonOperational => "NOP",
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
        };

        public static PxfStatus? FromV1(string code) => code switch
        {
            "1" => PxfStatus.Operational,
            "2" => PxfStatus.Limited,
            "3" => PxfStatus.NonOperational,
            _ => null,
        };

        public static PxfStatus? FromV2(string code) => code switch
        {
            "OPR" => PxfStatus.Operational,
            "LIM" => PxfStatus.Limited,
            "NOP" => PxfStatus.NonOperational,
            _ => null,
        };
    }
}
