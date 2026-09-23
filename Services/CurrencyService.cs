using Microsoft.AspNetCore.Http;

namespace AuraLiving.Services;

public class CurrencyInfo
{
    public string Code { get; set; } = "USD";
    public string Symbol { get; set; } = "$";
    public string Name { get; set; } = "US Dollar";
    public decimal RateFromUsd { get; set; } = 1.0m;
}

public interface ICurrencyService
{
    List<CurrencyInfo> GetSupportedCurrencies();
    string GetCurrentCurrencyCode();
    CurrencyInfo GetCurrentCurrency();
    void SetCurrentCurrency(string currencyCode);
    decimal ConvertFromUsd(decimal amountInUsd, string? targetCurrencyCode = null);
    string FormatPrice(decimal amountInUsd, string? targetCurrencyCode = null);
}

public class CurrencyService : ICurrencyService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    private static readonly Dictionary<string, CurrencyInfo> Currencies = new(StringComparer.OrdinalIgnoreCase)
    {
        { "USD", new CurrencyInfo { Code = "USD", Symbol = "$", Name = "US Dollar", RateFromUsd = 1.0m } },
        { "EUR", new CurrencyInfo { Code = "EUR", Symbol = "€", Name = "Euro", RateFromUsd = 0.92m } },
        { "GBP", new CurrencyInfo { Code = "GBP", Symbol = "£", Name = "British Pound", RateFromUsd = 0.78m } },
        { "INR", new CurrencyInfo { Code = "INR", Symbol = "₹", Name = "Indian Rupee", RateFromUsd = 83.50m } },
        { "AED", new CurrencyInfo { Code = "AED", Symbol = "AED ", Name = "UAE Dirham", RateFromUsd = 3.67m } }
    };

    public CurrencyService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public List<CurrencyInfo> GetSupportedCurrencies()
    {
        return Currencies.Values.ToList();
    }

    public string GetCurrentCurrencyCode()
    {
        var cookie = _httpContextAccessor.HttpContext?.Request.Cookies["indoria_currency"];
        if (!string.IsNullOrEmpty(cookie) && Currencies.ContainsKey(cookie))
        {
            return cookie.ToUpperInvariant();
        }
        return "USD";
    }

    public CurrencyInfo GetCurrentCurrency()
    {
        var code = GetCurrentCurrencyCode();
        return Currencies.TryGetValue(code, out var info) ? info : Currencies["USD"];
    }

    public void SetCurrentCurrency(string currencyCode)
    {
        if (string.IsNullOrEmpty(currencyCode) || !Currencies.ContainsKey(currencyCode))
            return;

        _httpContextAccessor.HttpContext?.Response.Cookies.Append("indoria_currency", currencyCode.ToUpperInvariant(), new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddYears(1),
            Path = "/",
            HttpOnly = false,
            IsEssential = true
        });
    }

    public decimal ConvertFromUsd(decimal amountInUsd, string? targetCurrencyCode = null)
    {
        var code = targetCurrencyCode ?? GetCurrentCurrencyCode();
        if (!Currencies.TryGetValue(code, out var info))
        {
            info = Currencies["USD"];
        }
        return Math.Round(amountInUsd * info.RateFromUsd, 2);
    }

    public string FormatPrice(decimal amountInUsd, string? targetCurrencyCode = null)
    {
        var code = targetCurrencyCode ?? GetCurrentCurrencyCode();
        if (!Currencies.TryGetValue(code, out var info))
        {
            info = Currencies["USD"];
        }

        decimal converted = ConvertFromUsd(amountInUsd, code);

        if (code == "INR")
        {
            return $"{info.Symbol}{converted:N0}";
        }

        return $"{info.Symbol}{converted:N2}";
    }
}
