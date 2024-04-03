using Script.Manager;
using UnityEngine;

namespace Script.GamePlay
{
    public class TouchDetectController : MonoBehaviour
    {
        public static TouchDetectController Inst;

        private Vector3 startPos;
        public Direction currentSwipeDirection = Direction.Up;
        [SerializeField] private Camera mainCam;
        internal VehicleController SelectedForCrane = null;

        private void Awake()
        {
            Inst = this;
        }

        private void Update()
        {
            if (GameplayScreen.Inst == null || GeneralDataManager.Inst.currentOpenedPopupList.Count>0) return;

            if (Input.GetMouseButtonDown(0))
            {
                if (SelectedForCrane == null)
                {
                    if (!GameplayScreen.Inst.Check_Can_Click_In_GamePlay() && !GameplayScreen.Inst.IsHammerPowerSelected) return;
                }
                else return;

                var ray = mainCam.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out var hit))
                {
                    var hitCollider = hit.collider;
                    if (hitCollider && hitCollider.gameObject.CompareTag("Vehicle"))
                    {
                        var vehicleController = hitCollider.gameObject.GetComponent<VehicleController>();
                        if (vehicleController.CheckCarCurrentState() == VehicleController.CarState.Running) return;

                        if (GameplayScreen.Inst.IsHammerPowerSelected)
                        {
                            SelectedForCrane = vehicleController;
                            return;
                        }

                        if (vehicleController.CarNumbers == HintPowerController.Inst.Get_Tutorial_Car()) HintPowerController.Inst.Check_Is_Hint();

                        startPos = Input.mousePosition;
                        GameplayManager.GetCarPath(vehicleController);
                    }
                }
            }
        }
    }
}