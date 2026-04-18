using System.IO;
using Il2CppRUMBLE.Managers;
using MelonLoader;
using MelonLoader.Utils;
using RumbleModdingAPI.RMAPI;
using UIFramework;
using UnityEngine;
using AudioManager = RumbleModdingAPI.RMAPI.AudioManager;
using BuildInfo = RumblePrecipitation.BuildInfo;
using Main = RumblePrecipitation.Main;

[assembly: MelonInfo(typeof(Main), BuildInfo.Name, BuildInfo.Version, BuildInfo.Author)]
[assembly: MelonGame("Buckethead Entertainment", "RUMBLE")]
[assembly: MelonColor(255, 255, 0, 0), MelonAuthorColor(255, 255, 0, 0)]
[assembly: MelonAdditionalDependencies("RumbleModdingAPI","UIFramework")]

namespace RumblePrecipitation;

public static class BuildInfo
{
    public const string Name = "Rumble Precipitation";
    public const string Author = "ERROR";
    public const string Version = "2.0.0";
}
    
public class Main : MelonMod
{
    private GameObject SFXParent;
    private GameObject VFXObject;
    private AudioSource sfxSource;
    private AudioSource muffledSfxSource;

    private const string USER_DATA = "UserData/RumblePrecipitation";
    private const string CONFIG_FILE = "config.cfg";

    public enum Weather
    {
        Rain,
        None
    }

    public MelonPreferences_Entry<Weather> selectedWeather;
    public MelonPreferences_Entry<float> outsideRainVolume;
    public MelonPreferences_Entry<float> insideRainVolume;
    
    public override void OnLateInitializeMelon()
    {
        Actions.onMapInitialized += OnMapInitialized;
        InitializeUI();
    }

    public void InitializeUI()
    {
        var precipitation = MelonPreferences.CreateCategory("Weather");

        selectedWeather = precipitation.CreateEntry("Selected_Weather", Weather.Rain, "Selected Weather", "Which weather to be shown.");        
        
        var rainSettings = MelonPreferences.CreateCategory("Rain");
        insideRainVolume = rainSettings.CreateEntry("Inside_Rain_Volume", 1.5f, "Inside Rain Volume", "The volume of the rain when under a roof.");
        outsideRainVolume = rainSettings.CreateEntry("Outside_Rain_Volume", 1.0f, "Outside Rain Volume", "The volume of the rain when under the sky.");
        
        var mod = UI.Register((MelonBase)this, precipitation, rainSettings);
        mod.OnModSaved += ReloadWeather;
    }

    public void OnMapInitialized(string sceneName)
    {
        ReloadWeather();
    }

    public void ReloadWeather()
    {
        if (SFXParent != null)
            GameObject.Destroy(SFXParent);
        
        if (VFXObject != null)
            GameObject.Destroy(VFXObject);

        if (selectedWeather.Value == Weather.Rain)
        {
            var rainBundle = AssetBundles.LoadAssetBundleFromStream(this, "RumblePrecipitation.Resources.rain5");
            VFXObject = GameObject.Instantiate(rainBundle.LoadAsset<GameObject>("Rain"));
            rainBundle.Unload(false);
        }
        
        LoadAudio();
    }

    public void LoadAudio()
    {
        SFXParent = new GameObject("Precipitation SFX");
        
        string path = Path.Combine(
            MelonEnvironment.UserDataDirectory,
            "RumblePrecipitation"
        );

        string audioPath = selectedWeather.Value switch
        {
            Weather.Rain => "Rain",
            _ => "None"
        };

        if (audioPath == "None")
            return;
        
        Directory.CreateDirectory(Path.GetDirectoryName(path));

        var sfxParent = new GameObject($"{audioPath} SFX");
        sfxParent.transform.SetParent(SFXParent.transform);
        
        var sfxPath = Path.Combine(path, $"{audioPath}.wav");
        var sfxMuffledPath = Path.Combine(path, $"{audioPath}_Muffled.wav");

        if (File.Exists(sfxPath))
        {
            var sfx = AudioManager.CreateAudioCall(sfxPath, 1.0f).clips[0].Clip;
            
            sfxSource = new GameObject($"{audioPath} SFX").AddComponent<AudioSource>();
            sfxSource.transform.SetParent(sfxParent.transform);
            
            sfxSource.clip = sfx;
            sfxSource.loop = true;
            sfxSource.volume = 0.0f;
            sfxSource.spatialBlend = 0.0f;
            sfxSource.Play();
        }
        else
        {
            LoggerInstance.Error($"'{audioPath}.wav' not found at path '{sfxPath}'");
        }
        
        if (File.Exists(sfxMuffledPath))
        {
            var rainMuffled = AudioManager.CreateAudioCall(sfxMuffledPath, 1.0f).clips[0].Clip;
            
            muffledSfxSource = new GameObject("Muffled Rain SFX").AddComponent<AudioSource>();
            muffledSfxSource.transform.SetParent(sfxParent.transform);
            
            muffledSfxSource.clip = rainMuffled;
            muffledSfxSource.loop = true;
            muffledSfxSource.volume = 0.0f;
            muffledSfxSource.spatialBlend = 0.0f;
            muffledSfxSource.Play();
        }
        else
        {
            LoggerInstance.Error($"'Rain_Muffled.wav' not found at path '{sfxMuffledPath}'");
        }
    }

    public override void OnUpdate()
    {
        if (sfxSource == null || muffledSfxSource == null)
            return;
        
        float cover = GetCoverAmount();

        float targetOutside = (1f - cover) * outsideRainVolume.Value;
        float targetInside = cover * insideRainVolume.Value;

        sfxSource.volume = Mathf.Lerp(sfxSource.volume, targetOutside, Time.deltaTime * 5f);
        muffledSfxSource.volume = Mathf.Lerp(muffledSfxSource.volume, targetInside, Time.deltaTime * 5f);
        
        Vector3 pos = PlayerManager.instance.LocalPlayer.Controller.PlayerCamera.camera.transform.position;
        VFXObject.transform.position = pos + new Vector3(0, 31.2f, 0);
    }

    float GetCoverAmount()
    {
        Vector3 pos = PlayerManager.instance.LocalPlayer.Controller.PlayerCamera.camera.transform.position;
        
        Vector3[] offsets = [
            Vector3.zero,
            new (0.5f, 0, 0),
            new (-0.5f, 0, 0),
            new (0, 0, 0.5f),
            new (0, 0, -0.5f)
        ];

        int hits = 0;

        foreach (var offset in offsets)
        {
            if (Physics.Raycast(pos + offset, Vector3.up, 5f, LayerMask.GetMask("Environment")))
                hits++;
        }

        return hits / (float)offsets.Length;
    }
}