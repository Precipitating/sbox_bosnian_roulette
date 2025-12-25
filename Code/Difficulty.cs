using Sandbox;

public enum Difficulty
{
	None = 0,
	Easy = 1,
	Normal = 2,
	Hard = 3,
}

public record BombAIDifficulty(
	Difficulty Difficulty,
	float MinReductionPercent,
	float MaxReductionPercent,
	float HazardExponent,
	float Variance,
	float MinDelay,
	float MaxDelay
);


public static class BombAIDifficultyConfig
{
	public static readonly IReadOnlyDictionary<Difficulty, BombAIDifficulty> All =
		new Dictionary<Difficulty, BombAIDifficulty>
		{
			[Difficulty.Easy] = new(
				Difficulty.Easy, 0.03f, 0.08f, 1.2f, 0.30f, 3.5f, 6.5f ),
			[Difficulty.Normal] = new(
				Difficulty.Normal, 0.05f, 0.20f, 1.8f, 0.25f, 1.5f, 3.5f ),
			[Difficulty.Hard] = new(
				Difficulty.Hard, 0.06f, 0.35f, 2.2f, 0.20f, 0.6f, 1.2f )
		};
}

public record MoveThreshold( float Easy, float Normal, float Hard );
public static class MoveThresholdConfig
{
	public static readonly MoveThreshold Yannow = new( 30f, 15f, 5f );
	public static readonly MoveThreshold Hollup = new( 200f, 100f, 50f );
	public static readonly MoveThreshold LeTrolle = new( 50f, 100f, 200f );
	public static readonly MoveThreshold Gandering = new( 100f, 70f, 50f );
	public static readonly MoveThreshold DoubleTroubleMin = new( 180f, 120f, 120f );
	public static readonly MoveThreshold DoubleTroubleMax= new( 40f, 60f, 80f );
}
