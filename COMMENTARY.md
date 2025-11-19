## Integration and Architecture
TradingSimulator integrates a lightweight AI forecasting layer directly into the Unity gameplay loop through the AITip and TradingManager components. The runtime coroutine AITip.RequestTip opens an HTTP POST to Ollama at http://localhost:11434/api/generate, sending a compact JSON payload describing the selected model (llama2 by default), temperature, and prompt. BuildPrompt assembles that prompt by combining static authoring instructions with the semantic tag from the shared MarketTrend enum so the large language model receives explicit context about whether the price is poised to rise, fall, or hold steady. TradingManager pre-rolls the next day's fish price before a turn ends, maps it to the enum, and asynchronously requests a heads-up sentence. This architecture keeps AI traffic off the main thread and ensures that the resulting prose references the future price that will actually be applied when the player clicks Change Day.

## Data Processed and Generated
The AI system processes textual capsules derived from deterministic simulation state. TradingManager samples the upcoming price with Random.Range, contrasts it against the current price, and reduces that delta into three narrative categories: shortage, surplus, or stability. That single categorical flag plus a hint about the day index becomes the entire prompt payload, so no raw telemetry or player identifiers ever leave the scene. UnityWebRequest posts the JSON and waits for text, and if nothing useful arrives AITip pivots to curated fallback arrays (shortageMessages, surplusMessages, steadyMessages) to synthesize deterministic prose. TradingManager then stores the forecasted price and message together, guaranteeing that UI copy and economic state stay perfectly aligned.

## Tools and Frameworks
This workflow combines several production tools. Unity coroutines (IEnumerator) provide nonblocking sequencing for both network operations and UI timing, while WaitForSeconds enforces a short display window before the next interaction. UnityWebRequest, UploadHandlerRaw, and DownloadHandlerBuffer keep the HTTP layer self-contained without external SDKs. TextMeshProUGUI renders the heads-up text inside an automatically created overlay canvas built with Canvas, CanvasScaler, and GraphicRaycaster, so designers do not need to wire UI prefabs manually. Ollama supplies the large language model runtime locally, letting the team experiment with llama2 or any compatible checkpoint without shipping new binaries. The integration is pure C#, so disabling the AI simply means toggling the useOllama boolean in the inspector.

## Gameplay and Workflow Impact
AI generated commentary materially enhances gameplay flow, creativity, and production. Forecasts that describe the next day's conditions rather than reiterating the present price turn routine buy or sell decisions into strategic gambles. Colorful prose about storms, gluts, or export runs gives players narrative hooks that make the minimalist UI feel alive even though the underlying economy remains a simple random walk. Designers save writing time because the LLM can inject varied phrasing, yet they retain total control through the fallback pools and prompt templates. Producers and QA benefit from the deterministic structure: they can unplug Ollama to validate graceful degradation, switch models for tone studies, or localize simply by swapping prompts. By delegating flavor text to an adjustable AI surface, TradingSimulator keeps the deterministic fish market authoritative while still delivering a dynamic, story driven experience.

# Implementation 

https://youtu.be/Uyr9s_8PHI0


## Bibliography
- Ollama. *Local AI model serving runtime*. https://ollama.com/ (accessed 2024).
