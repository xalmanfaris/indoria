using Microsoft.AspNetCore.Http;

namespace AuraLiving.Services;

public class CountryInfo
{
    public string Code { get; set; } = "US";
    public string Name { get; set; } = "United States";
    public string Flag { get; set; } = "🇺🇸";
    public string DefaultCurrency { get; set; } = "USD";
    public string DefaultLanguage { get; set; } = "en";
    public decimal ShippingRateUsd { get; set; } = 15.00m;
    public string EstimatedDays { get; set; } = "3-5 Business Days";
}

public interface ICountryService
{
    List<CountryInfo> GetSupportedCountries();
    string GetCurrentCountryCode();
    CountryInfo GetCurrentCountry();
    void SetCurrentCountry(string countryCode, bool syncPreferences = true);
}

public class CountryService : ICountryService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ICurrencyService _currencyService;
    private readonly ILocalizationService _localizationService;

    private static readonly Dictionary<string, CountryInfo> Countries = new(StringComparer.OrdinalIgnoreCase)
    {
        { "US", new CountryInfo { Code = "US", Name = "United States", Flag = "🇺🇸", DefaultCurrency = "USD", DefaultLanguage = "en", ShippingRateUsd = 15.00m, EstimatedDays = "2-4 Business Days" } },
        { "IN", new CountryInfo { Code = "IN", Name = "India", Flag = "🇮🇳", DefaultCurrency = "INR", DefaultLanguage = "hi", ShippingRateUsd = 5.00m, EstimatedDays = "3-5 Business Days" } },
        { "GB", new CountryInfo { Code = "GB", Name = "United Kingdom", Flag = "🇬🇧", DefaultCurrency = "GBP", DefaultLanguage = "en", ShippingRateUsd = 20.00m, EstimatedDays = "4-6 Business Days" } },
        { "DE", new CountryInfo { Code = "DE", Name = "Germany", Flag = "🇩🇪", DefaultCurrency = "EUR", DefaultLanguage = "de", ShippingRateUsd = 18.00m, EstimatedDays = "3-5 Business Days" } },
        { "AE", new CountryInfo { Code = "AE", Name = "United Arab Emirates", Flag = "🇦🇪", DefaultCurrency = "AED", DefaultLanguage = "ar", ShippingRateUsd = 12.00m, EstimatedDays = "2-3 Business Days" } },
        { "FR", new CountryInfo { Code = "FR", Name = "France", Flag = "🇫🇷", DefaultCurrency = "EUR", DefaultLanguage = "fr", ShippingRateUsd = 18.00m, EstimatedDays = "3-5 Business Days" } }
    };

    public CountryService(IHttpContextAccessor httpContextAccessor, ICurrencyService currencyService, ILocalizationService localizationService)
    {
        _httpContextAccessor = httpContextAccessor;
        _currencyService = currencyService;
        _localizationService = localizationService;
    }

    public List<CountryInfo> GetSupportedCountries()
    {
        return Countries.Values.ToList();
    }

    public string GetCurrentCountryCode()
    {
        var cookie = _httpContextAccessor.HttpContext?.Request.Cookies["indoria_country"];
        if (!string.IsNullOrEmpty(cookie) && Countries.ContainsKey(cookie))
        {
            return cookie.ToUpperInvariant();
        }
        return "US";
    }

    public CountryInfo GetCurrentCountry()
    {
        var code = GetCurrentCountryCode();
        return Countries.TryGetValue(code, out var info) ? info : Countries["US"];
    }

    public void SetCurrentCountry(string countryCode, bool syncPreferences = true)
    {
        if (string.IsNullOrEmpty(countryCode) || !Countries.ContainsKey(countryCode))
            return;

        var upperCode = countryCode.ToUpperInvariant();
        _httpContextAccessor.HttpContext?.Response.Cookies.Append("indoria_country", upperCode, new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddYears(1),
            Path = "/",
            HttpOnly = false,
            IsEssential = true
        });

        if (syncPreferences && Countries.TryGetValue(upperCode, out var info))
        {
            _currencyService.SetCurrentCurrency(info.DefaultCurrency);
            _localizationService.SetCurrentLanguage(info.DefaultLanguage);
        }
    }
}
