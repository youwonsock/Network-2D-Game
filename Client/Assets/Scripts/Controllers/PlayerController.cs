using Google.Protobuf.Protocol;
using System.Collections;
using UnityEngine;

public class PlayerController : CreatureController
{
	protected bool rangedSkill = false;
	protected Coroutine coSkill;

	protected override void Init()
	{
		base.Init();
	}

	protected override void UpdateAnimation()
	{
		if (animator == null || sprite == null)
			return;

		if (State == CreatureState.Idle)
		{
			switch (Dir)
			{
				case MoveDir.Up:
					animator.Play("IDLE_BACK");
					sprite.flipX = false;
					break;
				case MoveDir.Down:
					animator.Play("IDLE_FRONT");
					sprite.flipX = false;
					break;
				case MoveDir.Left:
					animator.Play("IDLE_RIGHT");
					sprite.flipX = true;
					break;
				case MoveDir.Right:
					animator.Play("IDLE_RIGHT");
					sprite.flipX = false;
					break;
			}
		}
		else if (State == CreatureState.Moving)
		{
			switch (Dir)
			{
				case MoveDir.Up:
					animator.Play("WALK_BACK");
					sprite.flipX = false;
					break;
				case MoveDir.Down:
					animator.Play("WALK_FRONT");
					sprite.flipX = false;
					break;
				case MoveDir.Left:
					animator.Play("WALK_RIGHT");
					sprite.flipX = true;
					break;
				case MoveDir.Right:
					animator.Play("WALK_RIGHT");
					sprite.flipX = false;
					break;
			}
		}
		else if (State == CreatureState.Skill)
		{
			switch (Dir)
			{
				case MoveDir.Up:
					animator.Play(rangedSkill ? "ATTACK_WEAPON_BACK" : "ATTACK_BACK");
					sprite.flipX = false;
					break;
				case MoveDir.Down:
					animator.Play(rangedSkill ? "ATTACK_WEAPON_FRONT" : "ATTACK_FRONT");
					sprite.flipX = false;
					break;
				case MoveDir.Left:
					animator.Play(rangedSkill ? "ATTACK_WEAPON_RIGHT" : "ATTACK_RIGHT");
					sprite.flipX = true;
					break;
				case MoveDir.Right:
					animator.Play(rangedSkill ? "ATTACK_WEAPON_RIGHT" : "ATTACK_RIGHT");
					sprite.flipX = false;
					break;
			}
		}
		else
		{

		}
	}

	protected override void UpdateController()
	{		
		base.UpdateController();
	}

	public override void UseSkill(int skillId)
	{
		if (skillId == 1)
		{
			coSkill = StartCoroutine("CoStartPunch");
		}
		else if (skillId == 2)
		{
			coSkill = StartCoroutine("CoStartShootArrow");
		}
	}

	protected virtual void CheckUpdatedFlag()
	{

	}

	IEnumerator CoStartPunch()
	{
		// 대기 시간
		rangedSkill = false;
		State = CreatureState.Skill;
		yield return new WaitForSeconds(0.5f);
		State = CreatureState.Idle;
		coSkill = null;
		CheckUpdatedFlag();
	}

	IEnumerator CoStartShootArrow()
	{
		// 대기 시간
		rangedSkill = true;
		State = CreatureState.Skill;
		yield return new WaitForSeconds(0.3f);
		State = CreatureState.Idle;
		coSkill = null;
		CheckUpdatedFlag();
	}
}
