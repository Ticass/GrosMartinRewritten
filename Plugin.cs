using BepInEx;
using BepInEx.Logging;
using System.IO;
using FusionModdingAPI.Module;
using GrosMartinRewritten.patches;
using UnityEngine;
using System.Collections.Generic;

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

    // Placement system fields
    private static bool _isPlacementMode = false;
    private static List<GameObject> _previewObjects = new List<GameObject>();
    private static List<Quaternion> _previewRotations = new List<Quaternion>();
    private static List<Vector3> _previewOffsets = new List<Vector3>();
    private static Vector3 _placementPosition;
    private const float PlacementDistance = 5f;
    private const float MoveSpeed = 2f;

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
        if (!Game.IsInGame)
            return;

        if (!_isPatched)
        {
            PatchFurniturePhone.Patch();
            _isPatched = true;
        }

        // Always update placement mode when in game
        UpdatePlacementMode();
    }

    private void UpdatePlacementMode()
    {
        if (!_isPlacementMode)
            return;

        _logger.LogInfo("UpdatePlacementMode running...");

        // Get player camera
        var playerCamera = Camera.main;
        if (playerCamera == null)
        {
            _logger.LogWarning("Camera.main is null, cancelling placement");
            CancelPlacement();
            return;
        }

        // Calculate placement position in front of player
        var forward = playerCamera.transform.forward;
        forward.y = 0; // Keep it horizontal
        forward.Normalize();
        
        // Raycast to ground
        RaycastHit hit;
        var rayStart = playerCamera.transform.position + forward * PlacementDistance;
        rayStart.y += 10f; // Start ray from above
        
        if (Physics.Raycast(rayStart, Vector3.down, out hit, 50f))
        {
            _placementPosition = hit.point;
        }
        else
        {
            // Fallback if no ground detected
            _placementPosition = playerCamera.transform.position + forward * PlacementDistance;
            _placementPosition.y = playerCamera.transform.position.y - 1f;
        }

        // Handle movement keys (WASD or Arrow keys)
        var moveInput = Vector3.zero;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            moveInput += forward;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            moveInput -= forward;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            moveInput -= playerCamera.transform.right;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            moveInput += playerCamera.transform.right;

        if (moveInput != Vector3.zero)
        {
            moveInput.y = 0;
            moveInput.Normalize();
            _placementPosition += moveInput * MoveSpeed * Time.deltaTime;
        }

        // Update preview positions
        UpdatePreviewPositions();

        // Confirm placement with Left Click or Enter
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            _logger.LogInfo("Confirm placement triggered");
            ConfirmPlacement();
        }

        // Cancel placement with Right Click or Escape
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            _logger.LogInfo("Cancel placement triggered");
            CancelPlacement();
        }
    }

    private static void UpdatePreviewPositions()
    {
        const float radius = 1.5f;
        const int count = 50;

        // Ensure we have the right number of preview objects
        while (_previewObjects.Count < count)
        {
            var newPreview = CreatePreviewObject();
            if (newPreview != null)
            {
                // Store a random rotation and offset for this piece
                _previewRotations.Add(Random.rotation);
                _previewOffsets.Add(new Vector3(
                    Random.Range(-radius, radius),
                    Random.Range(-radius, radius),
                    Random.Range(-radius, radius)
                ));
                _logger.LogInfo($"Created preview object {_previewObjects.Count}/{count}");
            }
        }

        // Update positions using STORED offsets (not new random ones each frame!)
        for (var i = 0; i < count && i < _previewObjects.Count; i++)
        {
            if (_previewObjects[i] == null)
            {
                _previewObjects[i] = CreatePreviewObject();
                if (i >= _previewRotations.Count)
                {
                    _previewRotations.Add(Random.rotation);
                }
                if (i >= _previewOffsets.Count)
                {
                    _previewOffsets.Add(new Vector3(
                        Random.Range(-radius, radius),
                        Random.Range(-radius, radius),
                        Random.Range(-radius, radius)
                    ));
                }
            }

            // Use the STORED offset, not a new random one
            var offset = (i < _previewOffsets.Count) ? _previewOffsets[i] : Vector3.zero;
            _previewObjects[i].transform.position = _placementPosition + offset;
            
            // Keep the rotation stable (use stored rotation)
            if (i < _previewRotations.Count)
            {
                _previewObjects[i].transform.rotation = _previewRotations[i];
            }
        }
    }

    private static GameObject CreatePreviewObject()
    {
        try
        {
            // Create a simple cube primitive for preview (no physics issues)
            var preview = GameObject.CreatePrimitive(PrimitiveType.Cube);
            
            // Scale it to look like a piece of firewood
            preview.transform.localScale = new Vector3(0.2f, 0.2f, 0.6f);
            // Rotation will be set in UpdatePreviewPositions
            
            // Destroy the collider completely
            var collider = preview.GetComponent<Collider>();
            if (collider != null)
            {
                Object.Destroy(collider);
            }
            
            // Make sure there's no rigidbody
            var rb = preview.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Object.Destroy(rb);
            }
            
            // Make it look like wood and make it transparent
            var renderer = preview.GetComponent<Renderer>();
            if (renderer != null)
            {
                var material = new Material(Shader.Find("Standard"));
                
                // Wood-like color
                material.color = new Color(0.6f, 0.4f, 0.2f, 0.5f);
                
                // Set material to transparent mode
                material.SetFloat("_Mode", 3);
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON");
                material.EnableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = 3000;
                
                renderer.material = material;
            }

            _previewObjects.Add(preview);
            return preview;
        }
        catch (System.Exception ex)
        {
            _logger.LogError($"Error creating preview object: {ex.Message}");
            return null;
        }
    }

    private static void ConfirmPlacement()
    {
        // Check if player can afford it
        if (Game.CashManager != null && !Game.CashManager.isCanAfford(350f))
        {
            _logger.LogWarning("Not enough money to complete purchase!");
            CancelPlacement();
            return;
        }

        // Deduct money
        Player.RemoveMoney(350, true);
        
        // Clear preview objects
        ClearPreviewObjects();

        // Spawn actual firewood
        SpawnFireWood(_placementPosition);

        _isPlacementMode = false;
        
        _logger.LogInfo("Wood pile placed successfully! 350$ deducted.");
    }

    private static void CancelPlacement()
    {
        // Clear preview objects
        ClearPreviewObjects();

        _isPlacementMode = false;
        
        _logger.LogInfo("Wood pile placement cancelled");
    }

    private static void ClearPreviewObjects()
    {
        foreach (var preview in _previewObjects)
        {
            if (preview != null)
            {
                Destroy(preview);
            }
        }
        _previewObjects.Clear();
        _previewRotations.Clear();
        _previewOffsets.Clear();
    }

    private static void SpawnFireWood(Vector3 basePosition)
    {
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
        // Start placement mode - money will be checked on confirmation
        _isPlacementMode = true;
        
        if (Camera.main != null)
        {
            _placementPosition = Camera.main.transform.position + Camera.main.transform.forward * PlacementDistance;
        }
        
        _logger.LogInfo("Placement mode activated! Use WASD to move, Left Click/Enter to confirm, Right Click/Escape to cancel");
    }

    private static void SpawnFireWood()
    {
        // Deprecated - use SpawnFireWood(Vector3) instead
        var basePosition = new Vector3(-1085.79f, 97.71f, -715.99f);
        SpawnFireWood(basePosition);
    }
}