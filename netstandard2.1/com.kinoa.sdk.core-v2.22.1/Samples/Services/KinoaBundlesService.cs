using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Kinoa.Data;
using Kinoa.Data.ResourceManagement;
using UnityEngine;

/// <summary>
///     Kinoa Bundles service.
/// </summary>
public class KinoaBundlesService : KinoaSingleton<KinoaBundlesService>
{
    /// <summary>
    ///     Gets bundle resources by one or more bundle keys.
    ///     Note: bundles attached to Feature Settings or In-app messages are already included
    ///     in their responses (SDK 2.9.0+) — no need to call this method in that case.
    /// </summary>
    /// <param name="bundleKeys">Collection of bundle keys.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    ///     Response with a dictionary of bundle resources.
    ///     Key = bundle key, value = collection of bundle resources.
    /// </returns>
    public async Task<Response<BundleResources>> GetBundleResourcesAsync(
        List<string> bundleKeys, CancellationToken cancellationToken = default)
    {
        if (bundleKeys == null || bundleKeys.Count == 0)
        {
            //TODO: Replace with your actual bundle keys defined on the Kinoa Dashboard.
            bundleKeys = new List<string> { "demoBundle" };
        }

        cancellationToken = EnsureCancellationToken(cancellationToken);

        var response = await Kinoa.Bundles.GetBundleResourcesAsync(bundleKeys, cancellationToken);
        if (response.IsSuccessful())
        {
            LogBundleResources(response.Data);
        }
        else if (response.IsConnectionError())
        {
            //TODO: Connection error handling.
            Debug.Log("[KINOA] Bundle resources request failed with connection error.");
        }
        else if (response.IsResponseFailed())
        {
            //TODO: Bad response handling.
            Debug.Log($"[KINOA] Bundle resources request failed: {response.Status}.");
        }
        else if (response.IsResponseCanceled())
        {
            //TODO: Cancellation handling.
            Debug.Log("[KINOA] Bundle resources request was canceled.");
        }

        return response;
    }

    /// <summary>
    ///     Ensures a valid CancellationToken (default: 5s timeout).
    /// </summary>
    private static CancellationToken EnsureCancellationToken(CancellationToken token)
    {
        if (token != default) return token;
        var cts = new CancellationTokenSource();
        cts.CancelAfter(5 * 1000);
        return cts.Token;
    }

    /// <summary>
    ///     Logs the bundle resources received from the server.
    ///     Demonstrates how to access BundleResources and Resource fields.
    /// </summary>
    private void Log(StringBuilder builder)
    {
        Debug.Log(builder.ToString());
        builder.Clear();
    }

    /// <summary>
    ///     Logs the bundle resources response.
    /// </summary>
    private void LogBundleResources(BundleResources data)
    {
        if (data?.BundleResourceBodiesDto == null || data.BundleResourceBodiesDto.Count == 0)
        {
            Debug.Log("[KINOA] The bundles are empty.");
            return;
        }

        var bundles = data.BundleResourceBodiesDto.Select(b =>
            $"\tBundle: {b.Key}\n\t\t" +
            string.Join("\n\t\t", b.Value.Select(r => $"{r.ResourceKey}: {r.Amount} (Body: {r.Body})")));

        var sb = new StringBuilder($"Bundles received ({data.BundleResourceBodiesDto.Count}):\n");
        sb.Append(string.Join("\n", bundles));

        Log(sb);
    }
}
