using System;
using System.Threading;
using System.Windows.Forms;

using BatRun;
using BatRun.Core;
using BatRun.UI;

namespace BatRun.UI
{
    // EN: Hosts HotkeySplashForm on a dedicated STA thread that has its own message pump
    // (Application.Run(form)). The controller (background) thread calls ShowSplashFor() and
    // returns IMMEDIATELY — no marshaling back to the main UI thread, no host form needed,
    // no taskbar entry leak. The splash lives for the specified duration then closes, which
    // exits Application.Run and ends the dedicated thread cleanly.
    // FR: Héberge HotkeySplashForm sur un thread STA dédié doté de sa propre pompe de messages
    // (Application.Run(form)). Le thread manette (background) appelle ShowSplashFor() et revient
    // IMMÉDIATEMENT — pas de marshaling vers le thread UI principal, pas besoin de form hôte,
    // pas de fuite d'entrée de barre des tâches. Le splash vit pour la durée spécifiée puis se
    // ferme, ce qui quitte Application.Run et termine proprement le thread dédié.
    internal static class HotkeySplashHost
    {
        public static void ShowSplashFor(TimeSpan duration)
        {
            // EN: Fire and forget: spawn an STA thread that runs the splash message pump.
            // The caller does NOT join on this thread.
            // FR: Fire-and-forget : démarre un thread STA qui fait tourner la pompe du splash.
            // L'appelant ne joint PAS ce thread.
            var splashThread = new Thread(() => RunSplash(duration))
            {
                IsBackground = true,
                Name = "BatRun HotkeySplash"
            };
            splashThread.SetApartmentState(ApartmentState.STA);
            splashThread.Start();
        }

        private static void RunSplash(TimeSpan duration)
        {
            try
            {
                // EN: Create the form on the dedicated STA thread so Show()/Close() work
                // properly. AutoResize is unnecessary; the form has fixed size and CenteScreen.
                // FR: Crée le form sur le thread STA dédié pour que Show()/Close() fonctionnent
                // correctement. Pas besoin d'auto-resize : le form a une taille fixe et un
                // StartPosition CenterScreen.
                using var splash = new HotkeySplashForm(duration);
                Application.Run(splash);
            }
            catch (Exception ex)
            {
                // EN: Best-effort log: never let the splash thread bring down BatRun.
                // FR: Log au mieux : ne jamais laisser le thread du splash planter BatRun.
                try { System.IO.File.AppendAllText(
                    System.IO.Path.Combine(AppContext.BaseDirectory, "Logs", "BatRun_error.log"),
                    $"[HotkeySplash] Thread crashed: {ex}\n"); } catch { }
            }
        }
    }
}
