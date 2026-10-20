using System;

namespace WIFISensitivitySearch.Services;

internal static class LicenseGuard
{
    // Build epoch — bump this when re-issuing a build with a fresh validity window.
    private static readonly DateTime BuildUtc = new DateTime(2026, 6, 26, 0, 0, 0, DateTimeKind.Utc);
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(259200);

    public static bool IsExpired => DateTime.UtcNow > BuildUtc + Ttl;
}
