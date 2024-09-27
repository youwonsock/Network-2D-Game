using Google.Protobuf.Protocol;

namespace Server.Game
{
	public class Player : GameObject
	{
		public ClientSession Session { get; set; }

		public Player()
		{
			ObjectType = GameObjectType.Player;
		}

		public override void OnDamaged(int damage)
		{
			base.OnDamaged(damage);
		}

		public override void OnDead()
		{
			base.OnDead();
		}
	}
}
