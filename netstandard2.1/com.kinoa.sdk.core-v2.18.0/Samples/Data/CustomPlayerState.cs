using System;
using System.Collections.Generic;
using Kinoa.Data.State;
using UnityEngine;

/// <summary>
///     Game-specific Player State extending <see cref="PlayerState"/> — used by Kinoa Dashboard
///     for audience segmentation, in-app trigger conditions, and Feature Settings filters.
/// </summary>
/// <remarks>
///     <para>
///     <b>Field selection:</b> add only fields with a clear Dashboard utility — don't mirror entire
///     game state. Each field needs (1) Player Field Path registered on Dashboard → Players
///     (snake_case), and (2) a write to <c>KinoaPlayerStateService.Instance.PlayerState.{Field}</c>
///     at every mutation site. Sync rides on the next game event (sync/async, carries the diff).
///     </para>
///     <para>
///     <b>Unregistered fields still ship from the client</b> — values reach the backend on each state
///     sync, but Dashboard cannot reference them in audiences/triggers/filters until registered.
///     Registration is optional for client correctness, mandatory for Dashboard utility.
///     </para>
///     <para>
///     Properties serialize via SnakeCaseLower (<c>CustomDateProperty</c> → <c>"custom_date_property"</c>);
///     dictionary keys as-is. Hydration on session-open: <see cref="KinoaPlayerStateService"/>'s
///     <c>GetLocalPlayerStateAsync</c> — local game state is the source of truth (offline-resilient).
///     </para>
/// </remarks>
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
