using Sandbox;
using System;
using System.Diagnostics;

public enum CardEnum
{
	None = 0,
	LeTrolle = 1,
	DoubleTrouble = 2,
	Hollup = 3,
	Yannow = 4,

}
public class Card
{
	public string Name { get; private set; }
	public string Description { get; private set; }
	public string Image { get; private set; }
	
	// more efficient doing comparison operations
	public CardEnum CardID = CardEnum.None;

	public Func<float, float> Activate { get;set; }

	public float Use( float inputTime ) => Activate?.Invoke( inputTime ) ?? inputTime;

	public Card( string name, string description, string imgPath, CardEnum cardID, Func<float, float> activate = null )
	{
		Name = name;
		Description = description;
		Image = imgPath;
		CardID = cardID;
		Activate = activate;
	}

}

public class CardDatabase
{
	public enum PersistingEffects
	{
		None = 0,
		ElTrolle = 1,
	}

	public CardDatabase(Bomb bombRef)
	{
		_bombRef = bombRef;
		_gameManager = GameManager.Instance;
		_cards = new List<Card>
		{
			new Card(
			"Le Trolle",
			"Falsely speed up the tick count for one turn",
			"ui/letrolle.png",
			CardEnum.LeTrolle,
			(inputTime)=>
			{
				Log.Warning($"Le Trolle activated with inputTime={inputTime}");
				_bombRef.LeTrolleModeToggle();
				return inputTime;


			}),
			new Card(
			"Double Trouble",
			"Double your input time",
			"ui/doubletrouble.png",
			CardEnum.DoubleTrouble,
			(inputTime)=>
			{
				Log.Warning($"Double Trouble activated");
				return inputTime *= 2f;


			}),
			new Card(
			"Hollup",
			"Double the current time",
			"ui/hollup.png",
			CardEnum.Hollup,
			(inputTime)=>
			{
				Log.Warning($"Hollup activated");
				return -_bombRef.Time + inputTime;
				
			}),
			new Card(
			"Yannow",
			"On this round, if the deduction results in the bomb time being EXACTLY 0, the other player loses instantly. Guess wrong? Add +100 to input",
			"ui/yannow.png",
			CardEnum.Yannow,
			(inputTime)=>
			{
				Log.Warning($"Yannow activated");
				if ((_bombRef.Time - inputTime) == 0)
				{
					_gameManager.SetUnoReversal(true);
					Log.Warning($"UNO REVERSED!");
				}
				else
				{
					inputTime += 100;
					SoundManager.Play2D("Wrong");
				}
				return inputTime;

			})
		};

	}
	private readonly List<Card> _cards;

	public IReadOnlyList<Card> Cards => _cards;

	public Card GetCard( int id ) => _cards[id];

	public Card GetRandomCard()
	{
		//return Cards[3];
		return Cards[Game.Random.Int( 0, Cards.Count - 1 )];

	}

	private Bomb _bombRef;
	private GameManager _gameManager;
}
