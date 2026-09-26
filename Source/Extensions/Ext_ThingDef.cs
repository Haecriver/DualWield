using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace DualWield
{
    public static class Ext_ThingDef
    {
        public static bool CanBeOffHand(this ThingDef td)
        {
            return td.IsWeapon 
                && DualWield.Settings.DualWieldSelection != null 
                && DualWield.Settings.DualWieldSelection.TryGetValue(td.defName, out Settings.Record value) 
                && value.isSelected; 
        }
        public static bool IsTwoHand(this ThingDef td)
        {
            return td.IsWeapon 
                && DualWield.Settings.TwoHandSelection != null 
                && DualWield.Settings.TwoHandSelection.TryGetValue(td.defName, out Settings.Record value) 
                && value.isSelected;
        }
    }
}
