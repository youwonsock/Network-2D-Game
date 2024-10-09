using Google.Protobuf.Protocol;
using UnityEngine;

public class BaseController : MonoBehaviour
{
	public int Id { get; set; }
    
	protected bool updated = false;
    protected Animator animator;
    protected SpriteRenderer sprite;

    private PositionInfo positionInfo = new PositionInfo();
    private StatInfo stat = new StatInfo();
	


	public virtual StatInfo Stat
	{
		get { return stat; }
		set
		{
			if (stat.Equals(value))
				return;

            stat.Hp = value.Hp;
            stat.MaxHp = value.MaxHp;
            stat.Speed = value.Speed;
		}
	}

	public float Speed
	{
		get { return Stat.Speed; }
		set { Stat.Speed = value; }
	}

	public virtual int Hp
	{
		get { return Stat.Hp; }
		set
		{
			Stat.Hp = value;
		}
	}

	public PositionInfo PosInfo
	{
		get { return positionInfo; }
		set
		{
			if (positionInfo.Equals(value))
				return;

			CellPos = new Vector3Int(value.PosX, value.PosY, 0);
			State = value.State;
			Dir = value.MoveDir;
		}
	}

	public void SyncPos()
	{
		Vector3 destPos = Managers.Map.CurrentGrid.CellToWorld(CellPos) + new Vector3(0.5f, 0.5f);
		transform.position = destPos;
	}

	public Vector3Int CellPos
	{
		get
		{
			return new Vector3Int(PosInfo.PosX, PosInfo.PosY, 0);
		}

		set
		{
			if (PosInfo.PosX == value.x && PosInfo.PosY == value.y)
				return;

			PosInfo.PosX = value.x;
			PosInfo.PosY = value.y;
			updated = true;
		}
	}

	public virtual CreatureState State
	{
		get { return PosInfo.State; }
		set
		{
			if (PosInfo.State == value)
				return;

			PosInfo.State = value;
			UpdateAnimation();
			updated = true;
		}
	}

	public MoveDir Dir
	{
		get { return PosInfo.MoveDir; }
		set
		{
			if (PosInfo.MoveDir == value)
				return;

			PosInfo.MoveDir = value;

			UpdateAnimation();
			updated = true;
		}
	}

	public MoveDir GetDirFromVec(Vector3Int dir)
	{
		if (dir.x > 0)
			return MoveDir.Right;
		else if (dir.x < 0)
			return MoveDir.Left;
		else if (dir.y > 0)
			return MoveDir.Up;
		else
			return MoveDir.Down;
	}

	public Vector3Int GetFrontCellPos()
	{
		Vector3Int cellPos = CellPos;

		switch (Dir)
		{
			case MoveDir.Up:
				cellPos += Vector3Int.up;
				break;
			case MoveDir.Down:
				cellPos += Vector3Int.down;
				break;
			case MoveDir.Left:
				cellPos += Vector3Int.left;
				break;
			case MoveDir.Right:
				cellPos += Vector3Int.right;
				break;
		}

		return cellPos;
	}

	protected virtual void UpdateAnimation()
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
					animator.Play("ATTACK_BACK");
					sprite.flipX = false;
					break;
				case MoveDir.Down:
					animator.Play("ATTACK_FRONT");
					sprite.flipX = false;
					break;
				case MoveDir.Left:
					animator.Play("ATTACK_RIGHT");
					sprite.flipX = true;
					break;
				case MoveDir.Right:
									animator.Play("ATTACK_RIGHT");
					sprite.flipX = false;
					break;
			}
		}
		else
		{

		}
	}

	void Start()
	{
		Init();
	}

	void Update()
	{
		UpdateController();
	}

	protected virtual void Init()
	{
		animator = GetComponent<Animator>();
		sprite = GetComponent<SpriteRenderer>();
		Vector3 pos = Managers.Map.CurrentGrid.CellToWorld(CellPos) + new Vector3(0.5f, 0.5f);
		transform.position = pos;

		UpdateAnimation();
	}

	protected virtual void UpdateController()
	{
		switch (State)
		{
			case CreatureState.Idle:
				UpdateIdle();
				break;
			case CreatureState.Moving:
				UpdateMoving();
				break;
			case CreatureState.Skill:
				UpdateSkill();
				break;
			case CreatureState.Dead:
				UpdateDead();
				break;
		}
    }

	protected virtual void UpdateIdle()
	{
	}

	protected virtual void UpdateMoving()
	{
		Vector3 destPos = Managers.Map.CurrentGrid.CellToWorld(CellPos) + new Vector3(0.5f, 0.5f);
		Vector3 moveDir = destPos - transform.position;

		float dist = moveDir.magnitude;
		if (dist < Speed * Time.deltaTime)
		{
			transform.position = destPos;
			MoveToNextPos();
		}
		else
		{
			transform.position += moveDir.normalized * Speed * Time.deltaTime;
			State = CreatureState.Moving;
		}
    }

	protected virtual void MoveToNextPos()
	{

	}

	protected virtual void UpdateSkill()
	{

	}

	protected virtual void UpdateDead()
	{

	}
}
