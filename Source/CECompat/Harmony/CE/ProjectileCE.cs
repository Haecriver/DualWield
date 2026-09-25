using CombatExtended;
using DualWield.Harmony;
using HarmonyLib;
using System;
using UnityEngine;
using Verse;

namespace DualWield.CECompat.Harmony
{
    // The same logic is applicable for CE
    [HarmonyPatch(typeof(ProjectileCE), nameof(ProjectileCE.Launch))]
    [HarmonyPatch(new Type[] { typeof(Thing), typeof(Vector2), typeof(Thing) })] // select main function
    static class ProjectileCE_Launch
    {
        static void Prefix(ref Thing launcher, ref Vector2 origin, Thing equipment)
        {
            Vector3 origin3 = new Vector3(origin.x, 0, origin.y);
            // Use the exact same code as the Projectile patch
            Projectile_Launch.Prefix(ref launcher, ref origin3, equipment);
            origin = new Vector2(origin3.x, origin3.z); // update origin
        }
    }
}
