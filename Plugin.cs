using BepInEx;
using BepInEx.Logging;
using System.IO;
using FusionModdingAPI.Module;
using GrosMartinRewritten.patches;
using UnityEngine;

namespace GrosMartinRewritten;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    private new static ManualLogSource _logger;
    
    [SerializeField]
    public static AudioClip Martine;

    private AssetBundle _martineAsset;
    private readonly string _assetBundlePath = Path.Combine(Paths.PluginPath, "GrosMartin", "Martine", nameof (Martine));
    private bool _isPatched;

    private void Awake()
    {
        // Plugin startup logic
        _logger = base.Logger;
        _logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        LoadAssetBundles();
    }

    private void LoadAssetBundles()
    {
        if (!File.Exists(_assetBundlePath))
        {
            _logger.LogWarning("Failed to load martine asset bundle");
        }
        else
        {
            
            _martineAsset = AssetBundle.LoadFromFile(_assetBundlePath);
            if (!_martineAsset)
            {
                _logger.LogWarning("Failed to load martine asset bundle");
            }
            else
            {
                Martine = _martineAsset.LoadAsset<AudioClip>("bois");
                Debug.Log(Martine);
            }
        }
    }

    public void Update()
    {
        if (!Game.IsInGame || _isPatched)
            return;
        PatchFurniturePhone.Patch();
        _isPatched = true;
    }

    private static void SpawnFireWood()
    {
        // Base spawn location
        var basePosition = new Vector3(-1085.79f, 97.71f, -715.99f);

        const float radius = 1.5f;
        const int count = 50;

        for (var i = 0; i < count; i++)
        {
            // Random offset around the base position
            var offset = new Vector3(
                Random.Range(-radius, radius),
                Random.Range(-radius, radius),
                Random.Range(-radius, radius)
            );

            var spawnPos = basePosition + offset;

            // Spawn firewood
            var spawnFirewood = World.SpawnFirewood((Tree_Type)1, true);
            spawnFirewood.transform.position = spawnPos;
        }
    }


    public static void HandleTransaction()
    {
        if (Game.CashManager != null && !Game.CashManager.isCanAfford(350f))
            return;
        Player.RemoveMoney(350, true);
        SpawnFireWood();
    }
}
