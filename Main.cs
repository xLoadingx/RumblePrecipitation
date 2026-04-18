using MelonLoader;
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
    public string currentScene = "Loader";

    public override void OnLateInitializeMelon()
    {
    }
    
    public void OnUIInit()
	{
        
	}

    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        currentScene = sceneName;
    }
}