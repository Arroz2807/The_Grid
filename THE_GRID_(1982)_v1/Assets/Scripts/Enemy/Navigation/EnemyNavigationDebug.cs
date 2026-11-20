#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Instrumentación de editor para medir la IA con números en lugar de
/// impresiones. No existe en builds: todo el archivo está bajo
/// UNITY_EDITOR, así que no agrega costo ni código al juego final.
///
/// Se instala sola al entrar en Play (RuntimeInitializeOnLoadMethod) y
/// dibuja un panel con, por comportamiento:
///   - decisiones tomadas
///   - porcentaje de decisiones que cayeron en el rescate/fallback
///   - porcentaje de errores deliberados (Random)
///   - muertes y promedio de pasos sobrevividos
///   - espacio promedio disponible en la mejor dirección (Random)
///   - porcentaje de intercepciones (Predict)
///   - libertad promedio que le quedó al jugador (Ambush)
///   - porcentaje de decisiones en modo Hold (Ambush)
///
/// Se usa para los escenarios de prueba T1–T8.
/// </summary>
public class EnemyNavigationDebug : MonoBehaviour
{
    public static bool ShowOverlay = true;

    private struct Stats
    {
        public int Decisions;
        public int Fallbacks;
        public int Mistakes;
        public int Deaths;
        public int TotalStepsAtDeath;
        public int TotalBestRoom;

        // Predict
        public int Interceptions;

        // Ambush
        public int FreedomSamples;
        public int TotalPlayerFreedom;
        public int Holds;
    }

    private static readonly Dictionary<string, Stats> StatsByBehavior =
        new Dictionary<string, Stats>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        GameObject host = new GameObject("~EnemyNavigationDebug");
        host.hideFlags = HideFlags.DontSave;
        DontDestroyOnLoad(host);
        host.AddComponent<EnemyNavigationDebug>();
    }

    public static void ResetStats()
    {
        StatsByBehavior.Clear();
    }

    /// <summary>
    /// Registra una decisión de comportamiento.
    ///
    /// bestRoom: espacio alcanzable de la mejor dirección (Random).
    /// usedFallback: ninguna dirección superó el umbral de supervivencia.
    /// usedMistake: se eligió a propósito una opción no filtrada.
    /// intercepted: se apuntó a un punto futuro del jugador (Predict).
    /// playerFreedom: celdas libres que le quedan al jugador con la celda
    ///                elegida bloqueada (-1 = no aplica) (Ambush).
    /// holding: el enemigo está sosteniendo una garganta (Ambush).
    /// </summary>
    public static void RecordDecision(
        string behaviorName,
        int bestRoom,
        bool usedFallback,
        bool usedMistake,
        bool intercepted = false,
        int playerFreedom = -1,
        bool holding = false)
    {
        StatsByBehavior.TryGetValue(behaviorName, out Stats stats);

        stats.Decisions++;
        stats.TotalBestRoom += bestRoom;

        if (usedFallback)
            stats.Fallbacks++;

        if (usedMistake)
            stats.Mistakes++;

        if (intercepted)
            stats.Interceptions++;

        if (holding)
            stats.Holds++;

        if (playerFreedom >= 0)
        {
            stats.TotalPlayerFreedom += playerFreedom;
            stats.FreedomSamples++;
        }

        StatsByBehavior[behaviorName] = stats;
    }

    /// <summary>
    /// Registra la muerte de un enemigo y cuántas celdas llegó a recorrer.
    /// </summary>
    public static void RecordDeath(string behaviorName, int stepsSurvived)
    {
        StatsByBehavior.TryGetValue(behaviorName, out Stats stats);

        stats.Deaths++;
        stats.TotalStepsAtDeath += stepsSurvived;

        StatsByBehavior[behaviorName] = stats;
    }

    private void OnGUI()
    {
        if (!ShowOverlay || StatsByBehavior.Count == 0)
            return;

        GUILayout.BeginArea(new Rect(8f, 8f, 640f, 34f + StatsByBehavior.Count * 20f), GUI.skin.box);

        GUILayout.Label("IA - percepcion (solo editor)");

        foreach (KeyValuePair<string, Stats> entry in StatsByBehavior)
        {
            Stats stats = entry.Value;

            float fallbackRate = stats.Decisions > 0
                ? 100f * stats.Fallbacks / stats.Decisions
                : 0f;

            float avgSteps = stats.Deaths > 0
                ? (float)stats.TotalStepsAtDeath / stats.Deaths
                : 0f;

            float avgRoom = stats.Decisions > 0
                ? (float)stats.TotalBestRoom / stats.Decisions
                : 0f;

            float mistakeRate = stats.Decisions > 0
                ? 100f * stats.Mistakes / stats.Decisions
                : 0f;

            float interceptionRate = stats.Decisions > 0
                ? 100f * stats.Interceptions / stats.Decisions
                : 0f;

            float avgFreedom = stats.FreedomSamples > 0
                ? (float)stats.TotalPlayerFreedom / stats.FreedomSamples
                : 0f;

            float holdRate = stats.Decisions > 0
                ? 100f * stats.Holds / stats.Decisions
                : 0f;

            string line =
                $"{entry.Key}  |  dec {stats.Decisions}  |  fallback {fallbackRate:0.0}%";

            if (stats.Mistakes > 0)
                line += $"  |  error {mistakeRate:0.0}%";

            if (stats.TotalBestRoom > 0)
                line += $"  |  room prom {avgRoom:0.0}";

            if (stats.Interceptions > 0)
                line += $"  |  intercep {interceptionRate:0.0}%";

            if (stats.FreedomSamples > 0)
                line += $"  |  libertad prom {avgFreedom:0.0}";

            if (stats.Holds > 0)
                line += $"  |  hold {holdRate:0.0}%";

            line += $"  |  muertes {stats.Deaths}  |  pasos/muerte {avgSteps:0.0}";

            GUILayout.Label(line);
        }

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Reset", GUILayout.Width(80f)))
            ResetStats();

        if (GUILayout.Button("Ocultar", GUILayout.Width(80f)))
            ShowOverlay = false;

        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }
}
#endif
