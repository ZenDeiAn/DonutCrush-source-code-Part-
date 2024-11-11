using Airpass.SensorManager;
using UnityEngine;

public class PlayerOB_Local : PlayerOB
{
    void FixedUpdate()
    {
        if (!IsLocal)
            return;
        
        if (GameManager.Instance.State != GameState.gaming)
            return;

        if (HP <= 0)
            return;

        MovingSpeed = SensorManager.Instance.GetVerticalValue(1);
        RotateAngle = SensorManager.Instance.GetHorizontalValue(1);
        // Process movement logic.
        this.MovementLogic(Time.fixedDeltaTime);
    }
}
