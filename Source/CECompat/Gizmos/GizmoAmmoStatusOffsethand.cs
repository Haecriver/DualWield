using CombatExtended;

namespace DualWield.CECompat.Gizmos
{
    public class GizmoAmmoStatusOffsethand : GizmoAmmoStatus
    {
        public override string Title => "(2) " + base.Title;

        public GizmoAmmoStatusOffsethand(GizmoAmmoStatus gizmoAmmoStatus): base()
        {
            compAmmo = gizmoAmmoStatus.compAmmo;
        }
    }
}
