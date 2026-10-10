using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace PartnerExchange.Contracts.Pxf;

internal static class PxfChecksum
{
    public static string Compute(string body)
    {
        var sum = 0;
        foreach (var _char in body)
            sum = (sum + _char) & 0xFFFF;
        return sum.ToString("X4", CultureInfo.InvariantCulture);
    }
}
