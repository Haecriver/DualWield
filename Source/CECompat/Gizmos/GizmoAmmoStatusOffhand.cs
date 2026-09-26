using CombatExtended;

namespace DualWield.CECompat.Gizmos
{
    public class GizmoAmmoStatusOffhand : GizmoAmmoStatus
    {
        public override string Title => "(2) " + base.Title;

        public GizmoAmmoStatusOffhand(GizmoAmmoStatus gizmoAmmoStatus): base()
        {
            compAmmo = gizmoAmmoStatus.compAmmo;
        }
    }
}
