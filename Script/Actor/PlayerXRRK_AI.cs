public class PlayerXRRK_AI : PlayerXRRK
{
    public override bool IsAI => true;

    public override void Initialize(int number)
    {
        this.InitializeLogic(number);
    }
}
