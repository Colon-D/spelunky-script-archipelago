using Archipelago.MultiClient.Net;
using System.Numerics;
using System.Runtime.InteropServices;

// SpecsHD and the Cheat Engine table were both used for addresses/structures/enums etc

namespace spelunky.scripts.archipelago.structs;

[StructLayout(LayoutKind.Explicit)]
public unsafe struct GlobalState {
	static private IntPtr _originalHelpAndOptionsMenuString;

	[StructLayout(LayoutKind.Explicit)]
	private unsafe struct Struct4C {
		[FieldOffset(0x122C50)]
		public IntPtr debug_menu_string;
		[FieldOffset(0x122C68)]
		public IntPtr help_and_options_menu_string;
	}

	[FieldOffset(0x4C)]
	private Struct4C* _struct_4C;

	public void EnableDebugMenu() {
		_originalHelpAndOptionsMenuString = _struct_4C->help_and_options_menu_string;
		_struct_4C->help_and_options_menu_string = _struct_4C->debug_menu_string;
	}

	public void DisableDebugMenu() {
		_struct_4C->help_and_options_menu_string = _originalHelpAndOptionsMenuString;
	}



	[FieldOffset(0x440606)]
	public byte in_worm;

	[FieldOffset(0x440684)]
	public Entity* player_1;
	[FieldOffset(0x440688)]
	public Entity* player_2;
	[FieldOffset(0x44068C)]
	public Entity* player_3;
	[FieldOffset(0x440690)]
	public Entity* player_4;

	[FieldOffset(0x440620)]
	public UInt32 killed_by_game_over; // game over screen
	[FieldOffset(0x4459FC)]
	public UInt32 killed_by_stats; // entity name

	[FieldOffset(0x445BE0)]
	public byte done_tutorial; // 0: no, 1: yes - but play animation, 2: yes - don't play animation

	[FieldOffset(0x44730A)]
	public byte display_path; // (when enabled, descriptions on menu do not show)
};

[StructLayout(LayoutKind.Explicit)]
public unsafe struct Entity {
	[FieldOffset(0x140)]
	public Int32 health;
	[FieldOffset(0x30)]
	public Vector2 position;
}

public enum JournalCategory {
	Places = 2,
	Monsters = 3,
	Items = 4,
	Traps = 5,
}

public enum JournalID {
	// places
	TheMines = 300,
	Jungle = 301,
	IceCaves = 302,
	Temple = 303,
	Hell = 304,
	// Cemetery = 305 // unused
	HauntedCastle = 306,
	BlackMarket = 307,
	Worm = 308,
	// YetiCave = 309 // unused 
	Mothership = 310,
	CityOfGold = 311,
	// monsters
	Snake = 1001,
	Cobra = 1036,
	Bat = 1003,
	Spider = 1002,
	SpinnerSpider = 1037,
	GiantSpider = 1018,
	Skeleton = 1012,
	Scorpion = 1029,
	Caveman = 1004,
	Damsel = 1005,
	Shopkeeper = 1006,
	TunnelMan = 1022,
	Scarab = 1024,
	TikiMan = 1041,
	Frog = 1007,
	FireFrog = 1021,
	GiantFrog = 1038,
	Mantrap = 1008,
	Piranha = 1013,
	OldBitey = 1023,
	KillerBee = 1032,
	QueenBee = 1034,
	Snail = 1043,
	Monkey = 1015,
	GoldenMonkey = 1050,
	JiangShi = 1019,
	GreenKnight = 1045,
	BlackKnight = 1049,
	Vampire = 1020,
	Ghost = 1017,
	Bacterium = 1035,
	WormEgg = 1046,
	WormBaby = 1047,
	Yeti = 1009,
	YetiKing = 1025,
	Mammoth = 1039,
	Alien = 1026,
	UFO = 1010,
	AlienTank = 1040,
	AlienLord = 1016,
	AlienQueen = 1048,
	HawkMan = 1011,
	CrocMan = 1044,
	MagmaMan = 1027,
	ScorpionFly = 1042,
	Mummy = 1014,
	Anubis = 1033,
	AnubisII = 1054,
	Olmec = 1055,
	Vlad = 1028,
	Imp = 1030,
	Devil = 1031,
	Succubus = 1051,
	HorseHead = 1052,
	OxFace = 1053,
	KingYama = 1056,
	// items
	RopePile = 500,
	BombBag = 501,
	BombBox = 502,
	Spectacles = 503,
	ClimbingGloves = 504,
	PitchersMitt = 505,
	SpringShoes = 506,
	SpikeShoes = 507,
	Paste = 508,
	Compass = 509,
	Mattock = 510,
	Boomerang = 511,
	Machete = 512,
	Crysknife = 513,
	WebGun = 514,
	Shotgun = 515,
	FreezeRay = 516,
	PlasmaCannon = 517,
	Camera = 518,
	Teleporter = 519,
	Parachute = 520,
	Cape = 521,
	Jetpack = 522,
	Shield = 523,
	RoyalJelly = 524,
	Idol = 525,
	Kapala = 526,
	UdjatEye = 527,
	Ankh = 528,
	Hedjet = 529,
	Scepter = 530,
	BookOfTheDead = 531,
	VladsCape = 532,
	VladsAmulet = 533,
	// traps
	Spikes = 100,
	ArrowTrap = 101,
	PowderBox = 102,
	Boulder = 103,
	TikiTrap = 104,
	Acid = 105,
	Spring = 106,
	Mine = 107,
	Turret = 108,
	Forcefield = 109,
	CrushTrap = 110,
	CeilingTrap = 111,
	// FlameTrap = ???, // unused
	SpikeBall = 113,
	Lava = 112,
}

public struct ArchipelagoItem {
	public string player;
	public string name;
}

public struct JournalCache {
	static public Dictionary<JournalID, ArchipelagoItem> map;
	static public async void InitMapAsync(ArchipelagoSession session) {
		map = new Dictionary<JournalID, ArchipelagoItem>();
		var journalIDs = Enum.GetValues<JournalID>();
		var journalIDsLong = journalIDs.Select(value => (long)value).ToArray();
		var result = await session.Locations.ScoutLocationsAsync(false, journalIDsLong);
		foreach (var loc in result.Locations) {
			map.Add(
				(JournalID)loc.Location,
				new ArchipelagoItem {
					player = session.Players.GetPlayerName(loc.Player),
					name = session.Items.GetItemName(loc.Item)
				}
			);
		}
	}
}

public enum EntityType {
	HiredHelp = 0, // crash
	RibCage = 11, // lame
	CeilingTrap = 42, // does nothing
	TrapDoor = 43, // does nothing
	SpringTrap = 44, // fun trap!!
	CrushTrap = 45, // fun trap but placement might be harder (exit door?)
	Landmine = 92, // fun (but too unfair?)
	Chest = 100, // money!
	Crate = 101, // items
	GoldBar = 102,
	GoldPyramid = 103,
	EmeraldLarge = 104,
	SapphireLarge = 105,
	RubyLarge = 106,
	LiveBomb = 107, // fun trap, more fair than a landmine
	DeployRope = 108,
	Whip = 109, // does nothing?
	Blood = 110, // could be good with kapala
	DirtBreak = 111, // particles
	Rock = 112,
	Pot = 113,
	Skull = 114,
	Cobweb = 115, // good trap?
	StickyHoney = 116, // particles
	Bullet = 117, // stationary bullet, probably need to manually apple velocity
	GoldNuggetLarge = 118,
	Boulder = 120, // good trap, just need to spawn above player (albeit, it is instant)
	PushBlock = 121,
	Arrow = 122,
	GoldNuggetSmall = 124,
	EmeraldSmall = 125,
	SapphireSmall = 126,
	RubySmall = 127,
	RopeSegment = 137, // does nothing
	CobwebProjectile = 142, // falls, then spawns Cobweb
	UdjatChest = 153, // could probably be hardcoded not to spawn
	GoldenKey = 154,  // could probably be hardcoded not to spawn
	UsedParachute = 156, // particle
	TikiSpikes = 157, // does nothing
	StaticSwingAttackProjectile = 158, // damages? but doesn't go away nor move
	PsychicAttackBubbling = 159, // particles (hopefully)
	UFOProjectile = 160,
	BlueFallingPlatform = 161,
	Lantern = 162,
	Flare = 163, // unused
	Snowball = 164,
	VomitFly = 165,
	WhiteFlag = 171, // flies a player, and takes camera with it?
	PiranhaSkeleton = 172, 
	Diamond = 173,
	WormTongue = 174, // generates glitched levels if not in jungle/ice caves
	MagmaCauldron = 176,
	WideLightEmitter = 177, // that light is wide and additive
	SpikeBallDetached = 178, // doesn't stop when started, like a boulder
	BreakingChainProjectile = 179, // projectile
	TutorialJournal = 180, // generates kind of glitched floors?
	JournalPage = 181, // particle?
	WormRegenBlock = 182,
	CrackingIcePlatform = 183,
	Leaf = 184, // particle?
	DecoyChest = 187, // visual only?
	PrizeWheel = 188, // shopkeepers not to happy if this is spawned when they exist, doesn't function?
	PrizeWheelPin = 189,
	PrizeWheelBarricade = 190,
	SnailBubble = 192,
	CobraVenomProjectile = 193,
	FallingIcicleProjectile = 194,
	BrokenIceProjectiles = 195, // particles
	SplashingWaterProjectile = 196, // particles
	ForcefieldGroundLaser = 197, // only works if spawned on ground
	ForcefieldLaser = 198, // nothing
	FreezeRayProjectile = 203, // stationary
	PlasmaCannonProjectile = 204, // nothing?
	MattockHead = 210,
	Coffin = 211, // glitched sprite inside
	TurretProjectile = 213, // stationary
	MothershipPlatform = 214, // nothing?
	MothershipElevator = 215,
	ArrowShaft = 216,
	OlmecEnemySpawnProjectile = 217, // spawns an enemy when it hits the ground, deals no damage though
	SplashingWater = 218, // particle
	BallAndChainWithoutChain = 220, // still attached to player though, one floor only
	SmokePoof = 221, // particles
	EndingCutsceneCamel = 224, // falls, has collision
	KillTarget = 225, // target for laser, then death
	ActivatedKillTargetLaser = 226, // just the death part of above
	MothershipLights = 227, // ceiling only, cosmetic essentially
	BrokenWebPouch = 228, // spawns a spider
	BreakingAnimation = 232, // particles
	MagmaFlameAnimation = 233, // particles
	AnubisIISpawner = 234, // persistant if anubis gets to spawn
	TNT = 235,
	SpinnerSpiderThread = 236, // particle
	DestroyedCobweb = 237, // particle
	DecoyYang = 239, // visual only
	ZeroValueGoldNugget = 240,
	LavaWaterSpout = 243, // crashes the game outside of appropriate areas. particles.
	MountedLightableTorchHolder = 245,
	UnlitTorch = 246,
	PurpleTarget = 247, // visual effect, maybe part of kill laser?
	UnopenableMysteryBox = 248,
	AlienQueenCorpse = 249, // can't see? has hitbox
	CrownedSkull = 250,
	Eggplant = 252,
	ExplodingAnimation = 301, // particles
	LaserEffectOrJetpackFlame = 302, // glitchy particle
	SmallLight = 303, // 1 frame particle
	SpringRings = 304, // particles
	TeleportEffect = 305, // particles
	WallTorchFlame = 306, // particles
	ExtinguishedTorchAnimation = 307, // particles
	RopePile = 500,
	BombBag = 501,
	BombBox = 502,
	Spectacles = 503,
	ClimbingGloves = 504,
	PitchersMitt = 505,
	SpringShoes = 506,
	SpikeShoes = 507,
	BombPaste = 508,
	Compass = 509,
	Mattock = 510,
	Boomerang = 511,
	Machete = 512,
	Crysknife = 513,
	WebGun = 514,
	Shotgun = 515,
	FreezeRay = 516,
	PlasmaCannon = 517,
	Camera = 518,
	Teleporter = 519,
	Parachute = 520,
	Cape = 521,
	Jetpack = 522,
	Shield = 523,
	QueenBeeRoyalJelly = 524,
	Idol = 525, // on caves: spawns boulders too, on jungle + temple: removes floor too, retextured to skull on cemetery (EVEN IF CHANGED TO ANOTHER ENTITY)
	Kapala = 526,
	UdjatEye = 527,
	Ankh = 528,
	Hedjet = 529,
	Scepter = 530,
	BookOfTheDead = 531, // spawns anubis II too
	VladsCape = 532,
	VladsAmulet = 533,
	Snake = 1001,
	Spider = 1002,
	Bat = 1003,
	Caveman = 1004,
	Damsel = 1005,
	Shopkeeper = 1006,
	BlueFrog = 1007,
	Mantrap = 1008,
	Yeti = 1009,
	UFO = 1010,
	HawkMan = 1011,
	Skeleton = 1012,
	Piranha = 1013, // instantly dies if not in water
	Mummy = 1014,
	Monkey = 1015,
	AlienLord = 1016,
	Ghost = 1017,
	GiantSpider = 1018,
	JiangShi = 1019,
	Vampire = 1020,
	OrangeFrog = 1021,
	TunnelMan = 1022,
	OldBitey = 1023,
	GoldenScarab = 1024,
	YetiKing = 1025,
	LittleAlien = 1026,
	MagmaMan = 1027, // instantly dies
	Vlad = 1028,
	Scorpion = 1029,
	Imp = 1030,
	BlueDevil = 1031,
	Bee = 1032,
	Anubis = 1033,
	QueenBee = 1034,
	Bacterium = 1035,
	Cobra = 1036,
	SpinnerSpider = 1037, // doesn't spawn with a web, but still acts like it does
	BigFrog = 1038,
	Mammoth = 1039,
	AlienTank = 1040,
	TikiMan = 1041,
	ScorpionFly = 1042,
	Snail = 1043,
	CrocMan = 1044,
	GreenKnight = 1045,
	WormEgg = 1046,
	WormBaby = 1047,
	AlienQueen = 1048,
	BlackKnight = 1049,
	GoldenMonkey = 1050,
	Succubus = 1051,
	HorseHead = 1052,
	OxFace = 1053,
	Olmec = 1055, // does not have textures loaded until temple at least
	KingYamaHead = 1056,
	KingYamaFist = 1057, // can't move if this is spawned
	Turret = 1058,
	BlueFrogCritter = 1059,
	AlienQueenEye = 1060, // particles?
	SpiderlingCritter = 1061,
	FishCritter = 1062, // dies out of water
	RatCritter = 1063,
	PenguinCritter = 1064,
	LittleAlienHorizontallyMoving = 1065, // particle?
	LocustCritter = 1067,
	MaggotCritter = 1068,
}

public struct EntityTypesEnabled {
	// keep up to date with python script
	// ideally I should find a way to automatically generate this set
	static public HashSet<EntityType> disabled = new HashSet<EntityType> {
		{ EntityType.RopePile },
		{ EntityType.BombBag },
		{ EntityType.BombBox },
		{ EntityType.Spectacles },
		{ EntityType.ClimbingGloves },
		{ EntityType.PitchersMitt },
		{ EntityType.SpringShoes },
		{ EntityType.SpikeShoes },
		{ EntityType.BombPaste },
		{ EntityType.Compass },
		{ EntityType.Mattock },
		{ EntityType.Boomerang }, // tikimen don't have boomerangs either
		{ EntityType.Machete },
		{ EntityType.Crysknife },
		{ EntityType.WebGun },
		{ EntityType.Shotgun }, // shopkeepers don't have shotguns either
		{ EntityType.FreezeRay },
		{ EntityType.PlasmaCannon },
		{ EntityType.Camera },
		{ EntityType.Teleporter },
		{ EntityType.Parachute },
		{ EntityType.Cape },
		{ EntityType.Jetpack },
		{ EntityType.Shield },
		{ EntityType.QueenBeeRoyalJelly },
		{ EntityType.Idol },
		{ EntityType.Kapala },
		{ EntityType.UdjatEye }, // should the player only need to unlock key + chest
		{ EntityType.Ankh },
		{ EntityType.Hedjet }, // should this be implicit to the moai statue?
		{ EntityType.Scepter }, // anubis doesn't need this to attack
		{ EntityType.BookOfTheDead },
		{ EntityType.VladsCape },
		{ EntityType.VladsAmulet },

		{ EntityType.Damsel },

		{ EntityType.UdjatChest },
		{ EntityType.GoldenKey },
	};
	static private Random _random = new Random();
	static public EntityType GetReplacementType() {
		// return random type from array
		var array = new[] {
			EntityType.Rock, EntityType.Pot, EntityType.Skull,
			EntityType.Arrow, EntityType.Snowball,
			EntityType.MattockHead, EntityType.PiranhaSkeleton,
			EntityType.MattockHead,
			EntityType.ArrowShaft, EntityType.UnlitTorch,
		};
		return array[_random.Next(array.Length)];
	}
}

struct ArchipelagoMessage {
	public string value;
	public UInt64 timestamp;
}
