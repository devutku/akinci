using Akinci.Domain.Enums;
using Akinci.Domain.ValueObjects;

namespace Akinci.UI.Localization;

/// <summary>
/// Güvenlik sonucunu arayüz dilinde rozet metnine çevirir. Çekirdekteki
/// <see cref="SecurityVerdict.FormatBadge"/> yalnızca Türkçe olduğu için arayüz onun yerine bunu kullanır.
/// </summary>
public static class SecurityVerdictText
{
    public static string Format(SecurityVerdict? verdict)
    {
        if (verdict == null)
        {
            return Strings.SecurityAwaitingScan;
        }

        switch (verdict.Reason)
        {
            case SecurityVerdictReason.NotFound:
                return Strings.SecurityNotFound;
            case SecurityVerdictReason.NoApiKey:
                return Strings.SecurityNoApiKey;
            case SecurityVerdictReason.RateLimited:
                return Strings.SecurityRateLimited;
            case SecurityVerdictReason.InvalidResponse:
                return Strings.SecurityInvalidResponse;
            case SecurityVerdictReason.ConnectionError:
                return Strings.SecurityConnectionError;
        }

        if (!verdict.IsScanned)
        {
            return Strings.SecurityAwaitingScan;
        }

        if (verdict.HasThreat)
        {
            return Strings.SecurityThreat(verdict.MaliciousVotes, verdict.TotalEngines);
        }

        return verdict.TotalEngines > 0
            ? Strings.SecurityClean(verdict.HarmlessVotes, verdict.TotalEngines)
            : Strings.SecurityNoEngineResults;
    }
}
