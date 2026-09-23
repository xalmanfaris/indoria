using AuraLiving.Services;
using Microsoft.AspNetCore.Mvc;

namespace AuraLiving.Controllers.Api;

[ApiController]
[Route("api/preferences")]
public class PreferencesApiController : ControllerBase
{
    private readonly ICurrencyService _currencyService;
    private readonly ILocalizationService _localizationService;
    private readonly ICountryService _countryService;

    public PreferencesApiController(
        ICurrencyService currencyService,
        ILocalizationService localizationService,
        ICountryService countryService)
    {
        _currencyService = currencyService;
        _localizationService = localizationService;
        _countryService = countryService;
    }

    [HttpGet("all")]
    public IActionResult GetAllPreferences()
    {
        return Ok(new
        {
            currencies = _currencyService.GetSupportedCurrencies(),
            activeCurrency = _currencyService.GetCurrentCurrency(),
            languages = _localizationService.GetSupportedLanguages(),
            activeLanguage = _localizationService.GetCurrentLanguage(),
            countries = _countryService.GetSupportedCountries(),
            activeCountry = _countryService.GetCurrentCountry(),
            isRtl = _localizationService.IsRtl()
        });
    }

    [HttpPost("currency")]
    public IActionResult SetCurrency([FromBody] SetPreferenceRequest req)
    {
        if (string.IsNullOrEmpty(req.Value)) return BadRequest(new { success = false, message = "Invalid currency code" });
        _currencyService.SetCurrentCurrency(req.Value);
        return Ok(new { success = true, activeCurrency = _currencyService.GetCurrentCurrency() });
    }

    [HttpPost("language")]
    public IActionResult SetLanguage([FromBody] SetPreferenceRequest req)
    {
        if (string.IsNullOrEmpty(req.Value)) return BadRequest(new { success = false, message = "Invalid language code" });
        _localizationService.SetCurrentLanguage(req.Value);
        return Ok(new { success = true, activeLanguage = _localizationService.GetCurrentLanguage(), isRtl = _localizationService.IsRtl() });
    }

    [HttpPost("country")]
    public IActionResult SetCountry([FromBody] SetPreferenceRequest req)
    {
        if (string.IsNullOrEmpty(req.Value)) return BadRequest(new { success = false, message = "Invalid country code" });
        _countryService.SetCurrentCountry(req.Value, syncPreferences: true);
        return Ok(new
        {
            success = true,
            activeCountry = _countryService.GetCurrentCountry(),
            activeCurrency = _currencyService.GetCurrentCurrency(),
            activeLanguage = _localizationService.GetCurrentLanguage()
        });
    }
}

public class SetPreferenceRequest
{
    public string Value { get; set; } = "";
}
