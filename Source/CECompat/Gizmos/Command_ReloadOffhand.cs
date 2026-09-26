using CombatExtended;
using Verse;

namespace DualWield.CECompat.Gizmos
{
    internal class Command_ReloadOffhand : Command_Reload
    {
        public override bool GroupsWith(Gizmo other)
        {
            return other is Command_ReloadOffhand;
        }

        public override void MergeWith(Gizmo other)
        {

        }
    }
}
