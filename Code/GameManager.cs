using Sandbox;
using Sandbox.UI;

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

	public void NextTurn( PanelComponent bombUI)
	{
		CurrentTurn = !CurrentTurn;
		bombUI.Enabled = !bombUI.Enabled;

		Log.Info( CurrentTurn );

	}

	protected override void OnStart()
	{
		Instance = this;
		AssignPlayer();
	}


	public static GameManager Instance { get; private set; }
	[Property] public GameObject Player1Camera { get; private set; }
	[Property] public GameObject Player2Camera { get; private set; }
	public bool CurrentTurn { get; private set; } = true;

	private bool Player1Assigned = false;
	private bool Player2Assigned = false;




}
