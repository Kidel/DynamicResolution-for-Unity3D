using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class DynamicResolution : MonoBehaviour
{
    /// <summary>
    /// Tells if the text has to be visible (true) or not (false).
    /// </summary>
    public bool EnableText = true;
    /// <summary>
    /// Disables everything when true.
    /// </summary>
    public bool DisableResolutionChangesAndEvaluation = false;
    /// <summary>
    /// Overrides the default resolution (1 means normal, 0.9f is 90%, and so on)
    /// </summary>
    public float StartingRatioOverride = 1;
    /// <summary>
    /// Setting to say that the current scene has static resolution (basically disables the script except for StartingRatioOverride)
    /// </summary>
    public bool StaticResolution = false;
    /// <summary>
    /// Setting to decide if the resolution has to be changed based on scene average fps or based on instant fps.
    /// </summary>
    public bool BasedOnSceneAvg = true;
    /// <summary>
    /// Setting to decide how many times a scene can chenge the resolution
    /// 0 = false; -1 = true (until minimum); 
    /// 1 means 1 time, 2 means 2 times and so on.
    /// </summary>
    public int InvokeRecursively = 1; // 0 = false; -1 = true; 1 means 1 time, 2 means 2 times and so on.
    /// <summary>
    /// Setting to tell if the resolution has to be changed at screen change or as soon as possible. 
    /// Enabled by default since resolution changes are visible. 
    /// Still changing all the time (false) can be useful if you have a scene dedicate to hardware estimation.
    /// </summary>
    public bool OnlyOnSceneChange = true;
    /// <summary>
    /// Resolution will go lower if FPS (or average FPS ot the scene, depending on settings) drop below this value
    /// </summary>
    public int InferiorFpsLimit = 29;
    /// <summary>
    /// Resolution will go higher if FPS (or average FPS ot the scene, depending on settings) goes above this value
    /// </summary>
    public int SuperiorFpsLimit = 49;
    /// <summary>
    /// Sets the maximum FPS of the scene (60 is usually the maximum allowed on smartphones)
    /// </summary>
    public int TargetFps = 60;

    private const float startingPeriod = 0.5f;
    private const float regimePeriod = 2.0f;
    private const float avgFpsStartPeriod = 2.0f; // better be a multiple of regimePeriod
    private bool startAvg = false;

    private static float fpsMeasurePeriod;
    private int fpsAccumulator = 0;
    private float fpsNextPeriod = 0;
    private int currentFps = 0;
    private int avgFpsAccumulator = 0;
    private float totalPeriodSinceStart = 0;
    private int currentAvgFps = 0;

    private static int lastSceneAvgFps = 0;

    const string display = "{0} FPS - {8} Avg FPS\nResolution at next refresh: {1}x{2}\nOriginal resolution: {3}x{4}\nCurrent res and modifier: {5}x{6} * {7}";
    private Text text;

    

    private static int originalwidth;
    private static int originalheight;
    private static int currentwidth;
    private static int currentheight;
    private int minimalwidth;
    private int minimalheight;

    public static bool resized { get; private set; }

    void Start()
    {
        this.gameObject.SetActive(!DisableResolutionChangesAndEvaluation);

        // initialization
        resized = resized ? true : false;
        fpsMeasurePeriod = startingPeriod;
        originalwidth = (originalwidth == 0) ? Screen.width : originalwidth;
        originalheight = (originalheight == 0) ? Screen.height : originalheight;

        currentwidth = (currentwidth == 0) ? originalwidth : currentwidth;
        currentheight = (currentheight == 0) ? originalheight : currentheight;

        minimalwidth = (int)(originalwidth * 0.69f);
        minimalheight = (int)(originalheight * 0.69f);

        float ratioOverride = (!resized) ? StartingRatioOverride : 1;

        fpsNextPeriod = Time.realtimeSinceStartup + fpsMeasurePeriod;

        text = GetComponent<Text>();

        // decisions
        if (EnableText)
            updateOverlay();
        else
            text.text = "";

        if (ratioOverride != 1 && (!resized || StaticResolution)) resize((int)(currentwidth * ratioOverride), (int)(currentheight * ratioOverride));
        else if (resized && OnlyOnSceneChange)
        {
            resize(currentwidth, currentheight);
            lastSceneAvgFps = 0;
        }
        if (!StaticResolution)
            if (InvokeRecursively == -1 || InvokeRecursively > 0)
            {
                Invoke("refreshResolution", regimePeriod);
            }
    }

    void Update()
    {
        // measuring frames per second in the fpsMeasurePeriod
        fpsAccumulator++;
        avgFpsAccumulator++;

        if (Time.realtimeSinceStartup > fpsNextPeriod)
        {
            totalPeriodSinceStart += fpsNextPeriod;
            int appFps = currentFps;
            currentFps = (int)(fpsAccumulator / fpsMeasurePeriod);
            if (appFps > currentFps) 
                startAvg = true;
            fpsAccumulator = 0;
            fpsNextPeriod += fpsMeasurePeriod;

            //Debug.Log("Time Since Level Load " + Time.timeSinceLevelLoad);
            if (BasedOnSceneAvg)
            {
                if (startAvg && Time.timeSinceLevelLoad > avgFpsStartPeriod)
                {
                    currentAvgFps = (int)(avgFpsAccumulator / Time.timeSinceLevelLoad);
                    lastSceneAvgFps = currentAvgFps;
                }
                else
                {
                    //Debug.Log("Not yet started recording avg fps");
                    currentAvgFps = currentFps;
                    avgFpsAccumulator = (int)(currentAvgFps * Time.timeSinceLevelLoad);
                }
            }
            if (EnableText)
                updateOverlay();
            else
                text.text = "";
        }
    }

    private void resize(int width, int height)
    {
        UnityEngine.Debug.Log(string.Format("resizing to {0}x{1}", width, height));
        Screen.SetResolution(width, height, true, TargetFps);
        resized = true;
    }

    private void refreshResolution()
    {
        // bases resolution change on the selected option, so scene average or current fps
        float fpsToUse = (BasedOnSceneAvg) ? (lastSceneAvgFps > 0 ? lastSceneAvgFps : currentAvgFps) : currentFps;
        bool changed = false;
        if (fpsToUse < InferiorFpsLimit && fpsToUse > 0 && currentheight > minimalheight)
        {
            float ratio = (resized) ? 0.92f : 0.82f;
            currentheight = (int)(currentheight * ratio);
            if (currentheight < minimalheight) currentheight = minimalheight;
            currentwidth = (int)(currentwidth * ratio);
            if (currentwidth < minimalwidth) currentwidth = minimalwidth;

            // if it's realtime dynamic resolution applies the resize
            if (!OnlyOnSceneChange) resize(currentwidth, currentheight);
            changed = true;
        }
        else if (resized && fpsToUse >= SuperiorFpsLimit && currentheight < originalheight)
        {
            currentheight = (int)(currentheight * 1.1f);
            if (currentheight > originalheight) currentheight = originalheight;
            currentwidth = (int)(currentwidth * 1.1f);
            if (currentwidth > originalwidth) currentwidth = originalwidth;

            // if it's realtime dynamic resolution applies the resize
            if (!OnlyOnSceneChange) resize(currentwidth, currentheight);
            changed = true;
        }

        resized = true;

        if (InvokeRecursively > 0 && changed)
        {
            InvokeRecursively--;
            changed = false;
        }
        if (InvokeRecursively == -1 || InvokeRecursively > 0)
        {
            Invoke("refreshResolution", regimePeriod);
        }
    }

    private void updateOverlay()
    {
        string settingsString = "\nMode: " + 
            (OnlyOnSceneChange ? "only at scene change" : "can change during scene") + 
            " " + 
            (InvokeRecursively == -1 ? "recursively" : (InvokeRecursively + " times") + 
            "\n      " + 
            (BasedOnSceneAvg ? "based on scene average":""));
        text.text = string.Format(display + settingsString, 
            currentFps, currentwidth, currentheight, originalwidth, originalheight, Screen.width, Screen.height, 
            !resized ? StartingRatioOverride : 1, currentAvgFps);
        Debug.Log(text.text);
    }
}

