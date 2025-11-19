using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using System.Collections;
using System;

public enum MarketTrend
{
    Unknown,
    PriceRising,
    PriceFalling,
    PriceStable
}

public class AITip : MonoBehaviour
{
    [Header("Ollama (local) settings")]
    public bool useOllama = true;
    public string ollamaUrl = "http://localhost:11434";
    public string ollamaModel = "llama2";

    [Header("Fallback messages (used when Ollama unavailable)")]
    [Tooltip("Used when the next day's price rises because supply is tight.")]
    public string[] shortageMessages = new string[]
    {
        "Boat damage limits supply; expect prices to climb.",
        "Storm keeps fleets docked; sellers raise prices.",
        "Export demand spikes overnight; costs jump at dawn.",
        "Fuel shortage trims voyages; traders warn of higher prices."
    };

    [Tooltip("Used when the next day's price falls thanks to abundant supply.")]
    public string[] surplusMessages = new string[]
    {
        "Nets bulge with fish; docks slash prices.",
        "Fleet returns early with a glut; bargains incoming.",
        "Cool currents herd massive schools; expect cheaper deals.",
        "Harbor overflows with catch; merchants undercut each other."
    };

    [Tooltip("Used when the next day's price barely changes.")]
    public string[] steadyMessages = new string[]
    {
        "Calm weather keeps catches steady; prices hold.",
        "Balanced hauls tonight; expect little change tomorrow.",
        "No surprises from the fleet; market stays level."
    };

    // Request a short tip. The provided callback will be invoked when the tip is ready.
    public IEnumerator RequestTip(MarketTrend trend, Action<string> onComplete)
    {
        string message = null;

        if (useOllama)
        {
            string prompt = BuildPrompt(trend);

            string json = $"{{\"model\":\"{ollamaModel}\",\"prompt\":\"{EscapeJson(prompt)}\",\"temperature\":0.7,\"max_tokens\":60}}";

            using (UnityWebRequest uwr = new UnityWebRequest(ollamaUrl + "/api/generate", "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                uwr.uploadHandler = new UploadHandlerRaw(bodyRaw);
                uwr.downloadHandler = new DownloadHandlerBuffer();
                uwr.SetRequestHeader("Content-Type", "application/json");

                yield return uwr.SendWebRequest();

                if (uwr.result == UnityWebRequest.Result.ConnectionError || uwr.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogWarning($"Ollama request failed: {uwr.error}");
                }
                else
                {
                    string resp = uwr.downloadHandler.text;
                    if (!string.IsNullOrEmpty(resp))
                    {
                        message = TryExtractTextFromOllamaResponse(resp);
                    }
                }
            }
        }

        if (string.IsNullOrEmpty(message))
        {
            message = GetFallbackMessage(trend);
        }

        onComplete?.Invoke(message);
    }

    private string BuildPrompt(MarketTrend trend)
    {
        string scenarioHint;
        switch (trend)
        {
            case MarketTrend.PriceRising:
                scenarioHint = "Supply is tight and the next day's fish price will rise.";
                break;
            case MarketTrend.PriceFalling:
                scenarioHint = "Harbors overflow with fish and the next day's price will fall.";
                break;
            case MarketTrend.PriceStable:
                scenarioHint = "Catches stay balanced so the next day's price will hold steady.";
                break;
            default:
                scenarioHint = "Describe a plausible fishing market update for the next day.";
                break;
        }

        return "You are an assistant that produces a single short, one-sentence, dramatic market-impact message about fishing supply. "
            + scenarioHint
            + " Match the tone to the movement (optimistic when prices fall, cautious when they rise) and do not include quotes.";
    }

    private string EscapeJson(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
    }

    private string TryExtractTextFromOllamaResponse(string resp)
    {
        try
        {
            int idx = resp.IndexOf("\"text\"");
            if (idx >= 0)
            {
                int colon = resp.IndexOf(':', idx);
                if (colon >= 0)
                {
                    int firstQuote = resp.IndexOf('"', colon + 1);
                    if (firstQuote >= 0)
                    {
                        int secondQuote = resp.IndexOf('"', firstQuote + 1);
                        if (secondQuote > firstQuote)
                        {
                            string extracted = resp.Substring(firstQuote + 1, secondQuote - firstQuote - 1);
                            return extracted.Trim();
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Failed to parse Ollama response: " + ex.Message);
        }

        return resp.Trim();
    }

    public string GetFallbackMessage(MarketTrend trend)
    {
        string[] pool = null;

        switch (trend)
        {
            case MarketTrend.PriceRising:
                pool = shortageMessages;
                break;
            case MarketTrend.PriceFalling:
                pool = surplusMessages;
                break;
            case MarketTrend.PriceStable:
                pool = steadyMessages;
                break;
            default:
                pool = steadyMessages;
                break;
        }

        if (pool == null || pool.Length == 0)
        {
            pool = shortageMessages;
            if (pool == null || pool.Length == 0)
            {
                pool = surplusMessages;
            }
        }

        if (pool != null && pool.Length > 0)
        {
            return pool[UnityEngine.Random.Range(0, pool.Length)];
        }

        return "Market update: no significant change.";
    }
}
