using System;
using System.Collections.Generic;
using Kinoa.Data.State;
using UnityEngine;

/// <summary>
///     The custom Player State <see cref="PlayerState"/> object extended with game-specific properties.
///     <remarks>
///     Properties are serialized using SnakeCaseLower naming policy (e.g., CustomDateProperty → "custom_date_property").
///     Dictionary keys are serialized as-is (no naming policy applied).
///     </remarks>
/// </summary>
public class CustomPlayerState : PlayerState
{
    /// <summary>
    ///     The custom Foo property.
    /// </summary>
    public string Foo { get; set; } = "Foo";

    /// <summary>
    ///     The custom Bar property.
    /// </summary>
    public List<string> Bar { get; set; } = new List<string>() { "Bar" };

    /// <summary>
    ///     The custom DateTime property.
    /// </summary>
    public DateTime? CustomDateProperty { get; set; }

    /// Custom type with a custom JSON converter. Requires JsonUtils.AddCustomConverter() before SDK.Initialize.
    /// See <see cref="KinoaSdkInitService"/> and <see cref="KinoaCustomJsonConverterSample"/>.
    //public CustomBool CustomBool { get; set; }

    // Unity-dependent property. SDK deserializes on a background thread — set Unity APIs from main thread only.
    //public int ScreenWidth { get; set; }

    //public void SetUnityProperties()
    //{
    //    ScreenWidth = Screen.width;
    //    Debug.Log($"The Unity properties are successfully set in the main thread: {ScreenWidth}.");
    //}
}
