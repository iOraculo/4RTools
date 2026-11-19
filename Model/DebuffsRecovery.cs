namespace _4RTools.Model
{
    public class DebuffsRecovery : StatusEffectRecoveryBase
    {
        public static string ACTION_NAME_DEBUFF_RECOVERY = "DebuffsRecovery";

        public override string GetActionName()
        {
            return ACTION_NAME_DEBUFF_RECOVERY;
        }
    }
}