using UnityEngine;

namespace Core.Services
{
    /// <summary>
    ///     Kinoa time service.
    /// </summary>
    public class KinoaTimeService : KinoaSingleton<KinoaTimeService>
    {
		
        public KinoaTimeService()
        {
            Kinoa.Time.OnKinoaServiceTimeUpdated += OnKinoaServiceTimeUpdated;
        }

		/// <summary>
        ///     Handler for service time update.
        /// </summary>
        private void OnKinoaServiceTimeUpdated()
        {
            var kinoaTime = Kinoa.Time.GetKinoaUnixTime(true);
            Debug.Log($"[GAME] OnKinoaServiceTimeUpdated: {kinoaTime.UnixTime} | IsServiceTime = {kinoaTime.IsServiceTime}");
        }
		
        /// <summary>
        ///     Checks if service time is available.
        /// </summary>
        public bool IsServiceTimeAvailable()
        {
            return Kinoa.Time.IsServiceTimeAvailable();
        }
    }
}