using Sandbox;
using System;


public enum CardEnum
{
	None = 0,
	LeTrolle = 1,
	DoubleTrouble = 2,
	Hollup = 3,
	Yannow = 4,
	Calma = 5,
	Gandering = 6,
	PassOff = 7,
	Shuffle = 8

}


public class Card
{
	public string Name { get; private set; }
	public string Description { get; private set; }
	public string Image { get; private set; }
	
	public CardEnum CardID = CardEnum.None;

	public Func<float, float> Activate { get;set; }

	/// <summary>
	/// Call the lambda assigned to the card if it exists.
	/// </summary>
	/// <param name="inputTime"></param>
	/// <returns>New input time after card is used</returns>
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
	/// <summary>
	/// If assigned, it means the card's activation will persist for X rounds.
	/// </summary>
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
				//Log.Warning($"Le Trolle activated with inputTime={inputTime}");
				_bombRef.LeTrolleModeToggle(true);
				return inputTime;


			}),
			new Card(
			"Double Trouble",
			"Double your input time",
			"ui/doubletrouble.png",
			CardEnum.DoubleTrouble,
			(inputTime)=>
			{
				//Log.Warning($"Double Trouble activated");
				return inputTime *= 2f;


			}),
			new Card(
			"Hollup",
			"Double the current time",
			"ui/hollup.png",
			CardEnum.Hollup,
			(inputTime)=>
			{
				//Log.Warning($"Hollup activated");
				return -_bombRef.Time + inputTime;
				
			}),
			new Card(
			"Yannow",
			"On activation, deduct enough for the resulting time to be exactly 0 to win instantly; guess wrong and receive +100s to your input.",
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

			}),
			new Card(
			"Calma",
			"On activation, input a safe number if your next input detonates the bomb",
			"ui/calma.jpg",
			CardEnum.Calma,
			(inputTime)=>
			{
				return (_bombRef.Time - inputTime <= 0) ? Game.Random.Int(1,int.Max(1, (int)_bombRef.Time - 1)) : inputTime;

			}),
			new Card(
			"Gandering",
			"Next input is checked if it's below the bomb time, emitting a sound if so. This action reduces 0 seconds.",
			"ui/gandering.png",
			CardEnum.Gandering,
			(inputTime)=>
			{
				if (_bombRef.Time < inputTime)
				{
					SoundManager.Play2D("IsBelow");
				}
				return 0;

			}),
			new Card(
			"Pass Off!",
			"Skip a turn",
			"ui/passoff.png",
			CardEnum.PassOff,
			(inputTime)=>
			{
				return 0;

			}),
			new Card(
			"Shuffle",
			"Change current card to a new random one",
			"ui/shuffle.jpg",
			CardEnum.Shuffle,
			(inputTime)=>
			{
				_gameManager.ChosenCard =  _cards[Game.Random.Int( 0, _cards.Count - 1 )];
				//Log.Info($"New card = {_gameManager.ChosenCard.Name}");
				return 0;

			})
		};

	}

	// public variables/properties
	public IReadOnlyList<Card> Cards => _cards;

	public Card GetCard( int id ) => _cards[id];

	public Card GetRandomCard()
	{
		//return Cards[7];
		return _cards[Game.Random.Int( 0, _cards.Count - 1 )];

	}

	// private variables
	private readonly List<Card> _cards;
	private readonly Bomb _bombRef;
	private readonly GameManager _gameManager;
}
