using HarmonyLib;
using UnityEngine;
namespace GrosMartinRewritten.patches;

[HarmonyPatch(typeof (Furniture_Phone), "RingingCompleted")]
public static class PatchFurniturePhoneRingingCompleted
{
    [HarmonyPrefix]
    public static bool Patch_RingingCompleted(ref Furniture_Phone __instance, ref int __result)
    {
        Debug.Log( "Ringing Completed");
        Debug.Log( "Number Composed: " + (string) AccessTools.Field(typeof (Furniture_Phone), "NumberComposed").GetValue(__instance));
        if ((string) AccessTools.Field(typeof (Furniture_Phone), "NumberComposed").GetValue(__instance) != "5146969")
            return true;
        Singleton<SOUND_Manager>.i.Play_Actor_OneShot(Plugin.Martine);
        Singleton<UI_EventMessage>.i.Show("Hey, need some wood ?, Tabarnak, I'll be there !", "Gros Martin", 8f, (UI_EventMessage.EventIconType) 1);
        __result = 1;
        Plugin.HandleTransaction();
        return false;
    }
}
