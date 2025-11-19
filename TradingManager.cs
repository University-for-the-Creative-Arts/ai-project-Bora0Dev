using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class TradingManager : MonoBehaviour
{
    
    //UI
    public TextMeshProUGUI dayText;
    public TextMeshProUGUI moneyText;
    public TextMeshProUGUI ownedText;   
    public TextMeshProUGUI priceText;  

    //fields

    public int money = 100;
    public int fishPrice = 10;
    public int amountOwned;
    public int dayCount = 1;

    // Reference to the AI tip provider (assign in Inspector)
    public AITip aiTip;

    // UI for AI heads-up (assign in Inspector or leave null to auto-create)
    public TextMeshProUGUI aiHeadsUpText;
    public float headsUpDuration = 25f;

    // When no AI tip provider is set we fall back to a minimal, hard-coded status line.
    private bool isAdvancingDay;
    private bool forecastReady;
    private bool forecastInProgress;
    private int forecastedFishPrice;
    private string forecastedMessage;

    private void Start()
    {
        UpdateUI();
        StartCoroutine(PrepareNextDayForecast());
    }

    public void BuyFish()
    {
        if (money >= fishPrice)
        {
            money -= fishPrice;
            amountOwned++;
        }
        UpdateUI();
    }

   public void sellFish()
    {
        if (amountOwned > 0)
        {
            amountOwned--;
            money += fishPrice;
        }
        UpdateUI();
    }

    public void UpdateUI()
    {
        if (dayText != null) dayText.text = $"Day: {dayCount}";
        if (moneyText != null) moneyText.text = $"Money: {money}";
        if (ownedText != null) ownedText.text = $"FishOwned: {amountOwned}";
        if (priceText != null) priceText.text = $"FishPrice: {fishPrice}";
        
    } 

    public void ChangeDate()
    {
        if (!isAdvancingDay)
        {
            StartCoroutine(AdvanceDayUsingForecast());
        }
    }

    private IEnumerator AdvanceDayUsingForecast()
    {
        isAdvancingDay = true;

        if (!forecastReady)
        {
            yield return StartCoroutine(PrepareNextDayForecast());
        }

        int targetPrice = forecastReady ? forecastedFishPrice : UnityEngine.Random.Range(1, 20);

        dayCount++;
        fishPrice = targetPrice;
        UpdateUI();

        forecastReady = false;
        forecastedMessage = null;

        StartCoroutine(PrepareNextDayForecast());

        isAdvancingDay = false;
    }

    private void ShowHeadsUp(string message)
    {
        EnsureHeadsUpUI();
        if (aiHeadsUpText != null)
        {
            aiHeadsUpText.text = message;
            aiHeadsUpText.gameObject.SetActive(true);
            StopCoroutine("ClearHeadsUpRoutine");
            StartCoroutine("ClearHeadsUpRoutine");
        }
        else
        {
            Debug.Log("AI Heads-up: " + message);
        }
    }

    private IEnumerator ClearHeadsUpRoutine()
    {
        yield return new WaitForSeconds(headsUpDuration);
        if (aiHeadsUpText != null)
        {
            aiHeadsUpText.text = string.Empty;
            aiHeadsUpText.gameObject.SetActive(false);
        }
    }

    private void EnsureHeadsUpUI()
    {
        if (aiHeadsUpText != null) return;

        Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("AIHeadsUpCanvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }

        GameObject textGO = new GameObject("AIHeadsUpText");
        textGO.transform.SetParent(canvas.transform, false);
        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.rectTransform.anchorMin = new Vector2(0.5f, 1f);
        tmp.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        tmp.rectTransform.pivot = new Vector2(0.5f, 1f);
        tmp.rectTransform.anchoredPosition = new Vector2(0f, -20f);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 28;
        tmp.color = Color.white;
        // rely on default wrapping settings
        tmp.gameObject.SetActive(false);

        aiHeadsUpText = tmp;
    }

    private IEnumerator PrepareNextDayForecast()
    {
        if (forecastInProgress)
        {
            while (forecastInProgress)
            {
                yield return null;
            }
            yield break;
        }

        forecastInProgress = true;

        int upcomingPrice = UnityEngine.Random.Range(1, 20);
        MarketTrend trend = DetermineTrend(fishPrice, upcomingPrice);
        string message = null;

        if (aiTip != null)
        {
            yield return StartCoroutine(aiTip.RequestTip(trend, msg => message = msg));

            if (string.IsNullOrEmpty(message))
            {
                message = aiTip.GetFallbackMessage(trend);
            }
        }

        if (string.IsNullOrEmpty(message))
        {
            message = GetLocalFallbackMessage(trend);
        }

        forecastedFishPrice = upcomingPrice;
        forecastedMessage = $"Day {dayCount + 1} forecast: {message}";
        forecastReady = true;

        ShowHeadsUp(forecastedMessage);

        forecastInProgress = false;
    }

    private MarketTrend DetermineTrend(int currentPrice, int nextPrice)
    {
        if (nextPrice > currentPrice) return MarketTrend.PriceRising;
        if (nextPrice < currentPrice) return MarketTrend.PriceFalling;
        return MarketTrend.PriceStable;
    }

    private string GetLocalFallbackMessage(MarketTrend trend)
    {
        switch (trend)
        {
            case MarketTrend.PriceRising:
                return "Supply crunch ahead; traders expect fish to cost more tomorrow.";
            case MarketTrend.PriceFalling:
                return "Boats report record hauls; cheaper fish is on the way.";
            default:
                return "Market update: no significant change.";
        }
    }
}
