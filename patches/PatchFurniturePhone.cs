namespace GrosMartinRewritten.patches;

using HarmonyLib;
using UnityEngine;

public abstract class PatchFurniturePhone
{
    // ReSharper disable Unity.PerformanceAnalysis
    public static void Patch()
    {
        new Harmony("com.crybaby.monbazou.grosmartin").PatchAll();
        Debug.Log("Phone Patched");
    }
}
