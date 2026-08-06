using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Kinoa.Data;
using Kinoa.Data.Enum;
using UnityEngine;

/// <summary>
///     Kinoa Currency Rates service.
/// </summary>
public class KinoaCurrencyRatesService : KinoaSingleton<KinoaCurrencyRatesService>
{
    #region API

    /// <summary>
    ///     Gets currency rates with string keys (e.g., "EUR", "GBP").
    /// </summary>
    /// <returns>Dictionary of currency code → USD exchange rate.</returns>
    public async Task<Response<Dictionary<string, double>>> GetStringCurrencyRatesAsync()
    {
        var response = await Kinoa.CurrencyRates.GetStringCurrencyRatesAsync();

        if (!response.IsSuccessful() || response.Data == null)
        {
            Debug.LogError($"[KINOA] Currency rates request failed: {response.Status}.");
            return response;
        }

        LogRates(response.Data.Select(r => $"{r.Key}: {r.Value}"));
        return response;
    }

    /// <summary>
    ///     Gets currency rates with <see cref="Currency"/> enum keys (e.g., Currency.EUR, Currency.USD).
    /// </summary>
    /// <returns>Dictionary of Currency enum → USD exchange rate.</returns>
    public async Task<Response<Dictionary<Currency, double>>> GetCurrencyRatesAsync()
    {
        var response = await Kinoa.CurrencyRates.GetCurrencyRatesAsync();

        if (!response.IsSuccessful() || response.Data == null)
        {
            Debug.LogError($"[KINOA] Currency rates request failed: {response.Status}.");
            return response;
        }

        LogRates(response.Data.Select(r => $"{r.Key}: {r.Value}"));
        return response;
    }

    #endregion

    #region Logging

    /// <summary>
    ///     Logs currency rates.
    /// </summary>
    private static void LogRates(IEnumerable<string> rates)
    {
        var ratesArray = rates.ToArray();
        Debug.Log($"[KINOA] Currency rates received ({ratesArray.Length}):\n\t{string.Join("\n\t", ratesArray)}");
    }

    #endregion
}
