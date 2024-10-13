using Google.Protobuf.Protocol;

public class MonsterController : CreatureController
{
	protected override void Init()
	{
		base.Init();
	}

	protected override void UpdateIdle()
	{
		base.UpdateIdle();
	}

	public override void UseSkill(int skillId)
	{
		if (skillId == 1)
		{
			State = CreatureState.Skill;
		}
	}
}
