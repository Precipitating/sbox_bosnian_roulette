using Sandbox;

public interface IGameManagerEvent : ISceneEvent<IGameManagerEvent>
{
	/// <summary>
	/// If this is called, GameManager is guaranteed to be fully initialized.
	/// </summary>
	void OnInitialized();
}
