using UnityEngine;
using UnityEngine.InputSystem;
using Smarc.GenericControllers;
using dji;

namespace SmarcGUI.KeyboardControllers
{
    [RequireComponent(typeof(DJIController))]
    public class DJIKeyboardController : KeyboardControllerBase
    {
        InputAction forwardAction, strafeAction, tvAction;
        DJIController djiCtrl;

        void Awake()
        {
            forwardAction = InputSystem.actions.FindAction("Robot/Forward");
            strafeAction = InputSystem.actions.FindAction("Robot/Strafe");
            tvAction = InputSystem.actions.FindAction("Robot/ThrustVector");
            
            djiCtrl = GetComponent<DJIController>();
        }

        void OnEnable(){}
        void OnDisable(){}
        public override void OnReset(){}

        void Update()
        {
            var forwardValue = forwardAction.ReadValue<float>();
            var strafeValue = strafeAction.ReadValue<float>();
            var tvValue = tvAction.ReadValue<Vector2>();
            var yawValue = -tvValue.x;
            var verticalValue = tvValue.y;

            if (Mathf.Abs(forwardValue) != 0 || Mathf.Abs(strafeValue) != 0 || Mathf.Abs(verticalValue) != 0 || Mathf.Abs(yawValue) != 0)
            {
                djiCtrl.CommandFLUYawRate01(forwardValue, -strafeValue, verticalValue, yawValue);
            }
        }


    }
}