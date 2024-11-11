using Airpass.SensorManager;
using UnityEngine;

public class PlayerXRRK_Local : PlayerXRRK
{
    void FixedUpdate()
    {
        if (GameManager.Instance.State != GameState.gaming)
            return;

        if (HP <= 0)
            return;
        
        MovingSpeed = SensorManager.Instance.GetVerticalValue(1);
        RotateAngle = SensorManager.Instance.GetHorizontalValue(1);
        // Process movement logic.
        this.MovementLogic(Time.fixedDeltaTime);
    }

    public override void Initialize(int number)
    {
        base.Initialize(number);
        
        GameManager.Instance.LocalPlayer = this;
    }
}
