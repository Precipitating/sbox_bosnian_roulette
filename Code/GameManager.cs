using Sandbox;
using Sandbox.Html;
using Sandbox.UI;
using System.Threading.Tasks;
public sealed class GameManager : Component
{
	public void SetCamera(GameObject cameraGameObject, string excludeName, ref bool assignVar)
	{
		cameraGameObject.Enabled = true;
		CameraComponent cam = cameraGameObject.GetComponent<CameraComponent>();
		cam.IsMainCamera = true;
		cam.RenderExcludeTags = new TagSet();
		cam.RenderExcludeTags.Add(excludeName);
		assignVar = true;

	}
	public void AssignPlayer()
	{
		if (!Player1Assigned)
		{
			SetCamera(Player1Camera, "player1", ref Player1Assigned );
			Player1Assigned = true;


		}
		else if (!Player2Assigned )
		{
			SetCamera( Player2Camera, "player2", ref Player2Assigned );
			Player2Assigned = true;

		}
	}

	public async Task<float> NextTurn()
	{
		if (!GameComplete )
		{
			CurrentTurn = !CurrentTurn;
			Log.Info( $"Is it your turn: {CurrentTurn}" );

			if ( !CurrentTurn && AIMode )
			{
				float enemyVal = await AIComponent.SimulateTurn();
				if ( GameComplete ) { return -1; }
				return enemyVal;
			}

		}
		return -1;

	}


	public void DetermineWinner()
	{
		if ( GameComplete ) { return; }
		GameComplete = true;
		YouWon = CurrentTurn ? false : true;
		CurrentTurn = false;

		Log.Warning( $"Did you win? {YouWon}" );

	}

	protected override void OnStart()
	{
		Instance = this;
		AssignPlayer();
		BombRef = Scene.Directory.FindByName( "BombModel" ).First().GetComponent<Bomb>();
		Log.Warning( $"MANAGER BombRef instance: {BombRef?.GetHashCode()}" );
	}

	public static GameManager Instance { get; private set; }
	[Property] public GameObject Player1Camera { get; private set; }
	[Property] public GameObject Player2Camera { get; private set; }
	[Property] public AiMode AIComponent;
	[Property] public Bomb BombRef { get; private set; }
	[Property] public bool YouWon { get; set; } = false;
	public bool AIMode = true;
	public bool CurrentTurn { get; set; } = true;
	public bool GameComplete { get; private set; } = false;

	private bool Player1Assigned = false;
	private bool Player2Assigned = false;





}
