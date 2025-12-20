using Sandbox;

public interface IGameManagerEvent : ISceneEvent<IGameManagerEvent>
{
	void OnInitialized();
}
