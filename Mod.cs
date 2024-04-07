using Reloaded.Mod.Interfaces;
using spelunky.script.archipelago.Template;
using spelunky.script.archipelago.Configuration;
using Reloaded.Hooks.Definitions;
using System.Diagnostics;
using DearImguiSharp;
using Reloaded.Imgui.Hook.Implementations;
using Reloaded.Imgui.Hook;
using CallingConventions = Reloaded.Hooks.Definitions.X86.CallingConventions;
using Reloaded.Hooks.Definitions.X86;
using static Reloaded.Hooks.Definitions.X86.FunctionAttribute;
using Reloaded.Hooks.Definitions.Enums;
using Reloaded.Memory;
using Reloaded.Memory.Interfaces;
using Reloaded.Memory.Utilities;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using System.Runtime.InteropServices;
using spelunky.scripts.archipelago.structs;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Helpers;
using SharpDX.Text;
using Reloaded.Memory.Pointers;
using System.Drawing;

namespace spelunky.script.archipelago;

/// <summary>
/// Your mod logic goes here.
/// </summary>
public class Mod : ModBase // <= Do not Remove.
{
	/// <summary>
	/// Provides access to the mod loader API.
	/// </summary>
	private readonly IModLoader _modLoader;

	/// <summary>
	/// Provides access to the Reloaded.Hooks API.
	/// </summary>
	/// <remarks>This is null if you remove dependency on Reloaded.SharedLib.Hooks in your mod.</remarks>
	private readonly IReloadedHooks _hooks;

	/// <summary>
	/// Provides access to the Reloaded logger.
	/// </summary>
	private readonly ILogger _logger;

	/// <summary>
	/// Entry point into the mod, instance that created this class.
	/// </summary>
	private readonly IMod _owner;

	/// <summary>
	/// Provides access to this mod's configuration.
	/// </summary>
	private Config _configuration;

	/// <summary>
	/// The configuration of the currently executing mod.
	/// </summary>
	private readonly IModConfig _modConfig;

	private unsafe GlobalState* _global;

	public static ImVec4 Color(uint hexColor) {
		byte red = (byte)((hexColor >> 24) & 0xFF);
		byte green = (byte)((hexColor >> 16) & 0xFF);
		byte blue = (byte)((hexColor >> 8) & 0xFF);
		byte alpha = (byte)(hexColor & 0xFF);

		float normalizedRed = red / 255.0f;
		float normalizedGreen = green / 255.0f;
		float normalizedBlue = blue / 255.0f;
		float normalizedAlpha = alpha / 255.0f;

		return new ImVec4 {
			X = normalizedRed,
			Y = normalizedGreen,
			Z = normalizedBlue,
			W = normalizedAlpha
		};
	}

	nint _game;

	const UInt32 _deathLinkMagicNumber = 0xFFFFFFFF;
	const int _killedByMaxLen = 64;
	Pinnable<char> _killedBy = new Pinnable<char>(new char[_killedByMaxLen]);
	const int _gameOverDescMaxLen = 256;
	Pinnable<char> _gameOverDesc = new Pinnable<char>(new char[_gameOverDescMaxLen]);
	const int _tryUnlockStringMaxLen = 128;
	Pinnable<char> _tryUnlockString = new Pinnable<char>(new char[_tryUnlockStringMaxLen]);
	const int _saveNameMaxLen = 512+1;
	Pinnable<byte> _saveName    = new Pinnable<byte>(new byte[_saveNameMaxLen]);
	Pinnable<byte> _saveNameTmp = new Pinnable<byte>(new byte[_saveNameMaxLen]);
	Pinnable<byte> _saveNameBak = new Pinnable<byte>(new byte[_saveNameMaxLen]);
	const int _testStringMaxLen = 256;
	Pinnable<char> _testString = new Pinnable<char>(new char[_testStringMaxLen]);
	Queue<ArchipelagoMessage> _stringQueue = new Queue<ArchipelagoMessage>(10);

	unsafe void Die(string killedBy, string gameOverDesc) {
		// todo: queue inbetween levels, disable in lobby
		if (_global->player_1 != null) {
			_global->player_1->health = 0;
		}
		if (_global->player_2 != null) {
			_global->player_2->health = 0;
		}
		if (_global->player_3 != null) {
			_global->player_3->health = 0;
		}
		if (_global->player_4 != null) {
			_global->player_4->health = 0;
		}
		SetPinnableString(_killedBy, killedBy, _killedByMaxLen);
		SetPinnableString(_gameOverDesc, gameOverDesc, _gameOverDescMaxLen);
		_global->killed_by_game_over = _deathLinkMagicNumber;
		_global->killed_by_stats = _deathLinkMagicNumber;
	}

	unsafe void SetPinnableString(Pinnable<char> pinnable, string str, int maxLen) {
		var arr = (str + "\0").ToCharArray();
		var len = Math.Min(arr.Length, maxLen);
		Marshal.Copy(arr, 0, (nint)pinnable.Pointer, len);
	}

	unsafe void SetPinnableString(Pinnable<byte> pinnable, string str, int maxLen) {
		var arr = Encoding.Default.GetBytes(str + "\0");
		var len = Math.Min(arr.Length, maxLen);
		Marshal.Copy(arr, 0, (nint)pinnable.Pointer, len);
	}

	public ArchipelagoSession session;
	public DeathLinkService deathLinkService;

	public void QueueMessage(string message) {
		while (_stringQueue.Count >= 10) {
			_stringQueue.Dequeue();
		}
		_stringQueue.Enqueue(new ArchipelagoMessage {
			value = message,
			timestamp = _frame
		});
	}

	public Mod(ModContext context) {
		_modLoader = context.ModLoader;
		_hooks = context.Hooks;
		_logger = context.Logger;
		_owner = context.Owner;
		_configuration = context.Configuration;
		_modConfig = context.ModConfig;

		_game = Process.GetCurrentProcess().MainModule.BaseAddress;
		// Archipelago
		session = ArchipelagoSessionFactory.CreateSession(_configuration.Hostname, _configuration.Port);

		deathLinkService = session.CreateDeathLinkService();

		LoginResult result;
		try {
			result = session.TryConnectAndLogin("Spelunky HD", _configuration.SlotName, ItemsHandlingFlags.AllItems);
		} catch (Exception e) {
			result = new LoginFailure(e.GetBaseException().Message);
		}

		if (!result.Successful) {
			LoginFailure failure = (LoginFailure)result;
			string errorMessage = $"Failed to Connect to {_configuration.Hostname}:{_configuration.Port} as {_configuration.SlotName}:";
			foreach (string error in failure.Errors) {
				errorMessage += $"\n    {error}";
			}
			foreach (ConnectionRefusedError error in failure.ErrorCodes) {
				errorMessage += $"\n    {error}";
			}
			Console.WriteLine(errorMessage);
			return;
		}
		var loginSuccess = (LoginSuccessful)result;

		JournalCache.InitMapAsync(session);

		deathLinkService.EnableDeathLink();
		deathLinkService.OnDeathLinkReceived += (deathLinkObject) => {
			var desc =
				"A deadly curse from beyond this world was cast upon me.\n("
				+ (deathLinkObject.Cause == null ? deathLinkObject.Source : deathLinkObject.Cause) + ")";
			Die(deathLinkObject.Source, desc);
		};

		foreach (var item in session.Items.AllItemsReceived) {
			EntityTypesEnabled.disabled.Remove((EntityType)item.Item);
		}
		session.Items.ItemReceived += (receivedItemsHelper) => {
			var item = receivedItemsHelper.DequeueItem();
			EntityTypesEnabled.disabled.Remove((EntityType)item.Item);
		};

		for (int i = 0; i < 10; ++i) {
			_stringQueue.Enqueue(new ArchipelagoMessage { value = "", timestamp = 0 });
		}
		session.MessageLog.OnMessageReceived += (message) => {
			var full_message = "";
			foreach (var part in message.Parts) {
				full_message += part.Text;
			}
			QueueMessage(full_message);
		};
		// End

		unsafe {
			MainLoopHook =
				_hooks.CreateHook<MainLoopHookDelegate>(MainHoopLoopImpl, _game + 0x673A0).Activate();




			string[] callGameOverAsm = {
				$"use32",
				$"pushad",
				$"{ _hooks.Utilities.GetAbsoluteCallMnemonics(GameOver, out GameOverWrapper) }",
				$"popad",
			};
			CallGameOverHook = _hooks.CreateAsmHook(callGameOverAsm, _game + 0x6C81A, AsmHookBehaviour.ExecuteFirst).Activate();



			using var gameOverTitle = new Pinnable<char>("Death Link".ToCharArray());
			string[] pushGameOverTitleAsm = {
				$"use32",
				$"pop ecx",
				$"push { (UIntPtr)gameOverTitle.Pointer }",
			};
			PushGameOverTitleHook = _hooks.CreateAsmHook(pushGameOverTitleAsm, _game + 0x9E68C, AsmHookBehaviour.ExecuteFirst).Activate();

			string[] pushGameOverDescAsm = {
				$"use32",
				$"pop eax",
				$"push { (UIntPtr)_gameOverDesc.Pointer }",
			};
			PushGameOverDescHook = _hooks.CreateAsmHook(pushGameOverDescAsm, _game + 0x9E6F9, AsmHookBehaviour.ExecuteFirst).Activate();

			string[] pushKilledByAsm = {
				$"use32",
				$"pop edx",
				$"push { (UIntPtr)_killedBy.Pointer }",
			};
			PushKilledByHook = _hooks.CreateAsmHook(pushKilledByAsm, _game + 0xAC524, AsmHookBehaviour.ExecuteFirst).Activate();



			ResetSaveHook = _hooks.CreateHook<ResetSaveDelegate>(ResetSave, _game + 0x8BCA0).Activate();
			// create archipelago save file (this does also reset settings though)
			// must be done before save load hook
			if (!_configuration.OriginalSave) {
				var saveName = $"Data/archipelago_save_{session.RoomState.Seed}.";
				SetPinnableString(_saveName, saveName + "sav", _saveNameMaxLen);
				SetPinnableString(_saveNameTmp, saveName + "tmp", _saveNameMaxLen);
				SetPinnableString(_saveNameBak, saveName + "bak", _saveNameMaxLen);
				var ReplaceSavePtr = (Pinnable<byte> filename, long op_addr) => {
					byte[] filename_ptr_bytes = new byte[4];
					byte* filename_ptr = filename.Pointer;
					byte** filename_ptr_ptr = &filename_ptr; // this feels dumb, I hate C#
					Marshal.Copy((IntPtr)filename_ptr_ptr, filename_ptr_bytes, 0, 4);
					Memory.Instance.SafeWrite((nuint)op_addr + 1, filename_ptr_bytes);
				};
				ReplaceSavePtr(_saveName, _game + 0x8C25D);
				ReplaceSavePtr(_saveName, _game + 0x8C273);
				ReplaceSavePtr(_saveName, _game + 0x8C280);
				ReplaceSavePtr(_saveName, _game + 0x8C296);
				ReplaceSavePtr(_saveName, _game + 0x8C2E1);
				ReplaceSavePtr(_saveNameTmp, _game + 0x8C1F6);
				ReplaceSavePtr(_saveNameTmp, _game + 0x8C200);
				ReplaceSavePtr(_saveNameTmp, _game + 0x8C285);
				ReplaceSavePtr(_saveNameTmp, _game + 0x8C29B);
				ReplaceSavePtr(_saveNameTmp, _game + 0x8C335);
				ReplaceSavePtr(_saveNameBak, _game + 0x8C24E);
				ReplaceSavePtr(_saveNameBak, _game + 0x8C258);
				ReplaceSavePtr(_saveNameBak, _game + 0x8C26E);
				ReplaceSavePtr(_saveNameBak, _game + 0x8C31A);
				ReplaceSavePtr(_saveNameBak, _game + 0x8C3A9);
			}



			LoadSaveHook =
				_hooks.CreateHook<LoadSaveDelegate>(LoadSaveImpl, _game + 0x8C2D0).Activate();



			TryUnlockHook = _hooks.CreateHook<TryUnlockDelegate>(TryUnlock, _game + 0x97220).Activate();



			// disable Steam (ie no leaderboards + achievements + daily)
			const byte nop = 0x90;
			for (nuint i = 0xE92E2; i < 0xE92F6; i++) {
				Memory.Instance.SafeWrite((nuint)_game + i, new[] { nop });
			}



			// spawn entity hook (to disable certain entities)
			SpawnEntityHook = _hooks.CreateHook<SpawnEntityDelegate>(SpawnEntity, _game + 0x70AB0).Activate();



			string[] pushTestStringAsm = {
				$"use32",
				$"pop edx",
				$"push { (UIntPtr)_testString.Pointer }",
			};
			_hooks.CreateAsmHook(pushTestStringAsm, _game + 0x91C1B, AsmHookBehaviour.ExecuteFirst).Activate();
			Memory.Instance.SafeWrite((nuint)_game + 0x91C3A, new[] { nop, nop, nop, nop, nop, nop }); // don't for loop
		}
	}

	public IAsmHook CallGameOverHook;
	[Function(CallingConventions.Stdcall)]
	public unsafe delegate void GameOverDelegate();
	public IReverseWrapper<GameOverDelegate> GameOverWrapper;
	
	public IAsmHook PushGameOverTitleHook;
	public IAsmHook PushGameOverDescHook;
	public IAsmHook PushKilledByHook;

	public IAsmHook MovEbxDebugMenuAsmHook;
	public IAsmHook MovEbxDebugMenuAsmHook2;

	public IAsmHook TryUnlockStringHook;

	bool _imgui_open = true;
	bool _debugMenuEnabled = false;
	private void Imgui() {
		if (!ImGui.Begin(
			"Debugger",
			ref _imgui_open,
			(int)ImGuiWindowFlags.NoResize | (int)ImGuiWindowFlags.NoMove
		)) {
			return;
		}
		ImGui.SetWindowPosVec2(new ImVec2 { X = 8, Y = 8 }, 0);
		ImGui.SetWindowSizeVec2(new ImVec2 { X = 192 + 96, Y = 0 }, 0);

		unsafe {
			if (ImGui.Button("Die", new ImVec2 { X = 0, Y = 0 })) {
				Die("Debugging", "A mysterious button set my health to zero.");
			}
			if (ImGui.Checkbox("Enable Debug Menu", ref _debugMenuEnabled)) {
				if (_debugMenuEnabled) {
					_global->EnableDebugMenu();
				} else {
					_global->DisableDebugMenu();
				}
			}
			foreach (var entity in Enum.GetValues<EntityType>()) {
				if (ImGui.Button($"Spawn {entity}", new ImVec2 { X = 0, Y = 0 })) {
					var pos = _global->player_1->position;
					SpawnEntity((int)_global, pos.X + 1, pos.Y, entity, (char)1);
				}
			}
		}
	}

	bool _mainLoopOnce = true;
	public unsafe void MainLoopOnce() {
		// init other stuff
		_global = *(GlobalState**)(_game + 0x15446C);

		// enable debug text
		_global->display_path = 1;

		// initialize Dear ImGui
		if (!_configuration.EnableImGui) {
			return;
		}
		SDK.Init(_hooks);
		ImguiHook.Create(Imgui, new ImguiHookOptions() {
			Implementations = new List<IImguiHook>() {
				new ImguiHookDx9()
			}
		}).ConfigureAwait(false);

		// don't write imgui file
		ImGui.GetIO().IniFilename = null;
		// PRESENTATION!
		var style = ImGui.GetStyle();
		var colors = style.Colors;
		colors[(int)ImGuiCol.WindowBg] = Color(0x7F7F7FBF);
		colors[(int)ImGuiCol.Button] = Color(0x9F9F9FFF);
		colors[(int)ImGuiCol.ButtonHovered] = Color(0xBFBFBFFF);
		colors[(int)ImGuiCol.ButtonActive] = Color(0xDFDFDFFF);
		colors[(int)ImGuiCol.CheckMark] = Color(0xFFFFFFFF);
		colors[(int)ImGuiCol.FrameBg] = Color(0x9F9F9FFF);
		colors[(int)ImGuiCol.FrameBgHovered] = Color(0xBFBFBFFF);
		colors[(int)ImGuiCol.FrameBgActive] = Color(0xDFDFDFFF);
		colors[(int)ImGuiCol.Separator] = Color(0x9F9F9FBF);
		colors[(int)ImGuiCol.Text] = Color(0xFFFFFFFF);
		colors[(int)ImGuiCol.TitleBg] = Color(0x9F9F9FFF);
		colors[(int)ImGuiCol.TitleBgActive] = Color(0x9F9F9FFF);
		colors[(int)ImGuiCol.TitleBgCollapsed] = Color(0x9F9F9FFF);
		style.Colors = colors;
		style.WindowBorderSize = 0f;
		style.WindowRounding = 4f;
		style.FrameRounding = 4f;
		// ImGui end, do not append here (because of _configuration.EnableImGui)
	}

	public unsafe void GameOver() {
		if (_global->killed_by_game_over == _deathLinkMagicNumber) {
			PushGameOverTitleHook.Enable();
			PushGameOverDescHook.Enable();
			PushKilledByHook.Enable();
		} else {
			PushGameOverTitleHook.Disable();
			PushGameOverDescHook.Disable();
			PushKilledByHook.Disable();
			string cause = null;
			switch (_global->killed_by_game_over) {
				case 0x1002:
				case 0x1037:
					cause = $"{_configuration.SlotName} succumbed to a spider bite.";
					break;
				case 0x1003:
					cause = $"A bat nibbled {_configuration.SlotName} to death.";
					break;
				case 0x1004:
					cause = $"A caveman pummeled {_configuration.SlotName} to death.";
					break;
				case 0x1006:
				case 0x1215:
					cause = $"{_configuration.SlotName} should never have angered that shopkeeper...";
					break;
				case 1007:
				case 1021:
				case 1038:
					cause = $"{_configuration.SlotName} got squished by a frog.";
					break;
				case 1008:
					cause = $"{_configuration.SlotName} was devoured by a mantrap.";
					break;
				case 1009:
					cause = $"A yeti pummeled {_configuration.SlotName} to death.";
					break;
				case 1010:
				case 1210:
					cause = $"{_configuration.SlotName} has been incinerated by a UFO laser blast.";
					break;
				case 1011:
					cause = $"A hawk man pummeled {_configuration.SlotName} to death.";
					break;
				case 1012:
					cause = $"{_configuration.SlotName} was clawed to death by a skeleton.";
					break;
				case 1013:
					cause = $"{_configuration.SlotName} was nibbled to death by a piranha.";
					break;
				case 1014:
					cause = $"An ancient mummy has gotten its revenge on {_configuration.SlotName}.";
					break;
				case 1016:
				case 1048:
				case 1216:
					cause = $"A powerful psychic blast has liquified {_configuration.SlotName}'s brains.";
					break;
				case 1017:
					cause = $"Where is {_configuration.SlotName}? {_configuration.SlotName} can't feel their body...";
					break;
				case 1018:
					cause = $"That was the biggest spider {_configuration.SlotName} has ever seen!";
					break;
				case 1019:
					cause = $"A jiang shi has leeched away {_configuration.SlotName}'s life energy.";
					break;
				case 1020:
					cause = $"{_configuration.SlotName} can feel their humanity draining away from the vampire's bite...";
					break;
				case 1023:
					cause = $"{_configuration.SlotName} was torn apart by the legendary river monster.";
					break;
				case 1025:
					cause = $"{_configuration.SlotName} has been thrashed by the king of the yetis.";
					break;
				case 1026:
					cause = $"Embarrassingly enough, {_configuration.SlotName} has been killed by a little alien.";
					break;
				case 1027:
					cause = $"{_configuration.SlotName} was burned to death by a magma man.";
					break;
				case 1028:
					cause = $"{_configuration.SlotName} confronted Vlad the Impaler and lost.";
					break;
				case 1029:
					cause = $"{_configuration.SlotName} was stung by a scorpion!";
					break;
				case 1030:
					cause = $"{_configuration.SlotName} was nibbled to death by a minion of Hell!";
					break;
				case 1031:
					cause = $"A blue devil perforated {_configuration.SlotName} with one of his horns.";
					break;
				case 1032:
				case 1034:
					cause = $"{_configuration.SlotName} must be allergic to bees.";
					break;
				case 1033:
					cause = $"{_configuration.SlotName} has been destroyed by the jackal-headed god Anubis.";
					break;
				case 1036:
					cause = $"{_configuration.SlotName} succumbed to deadly snake venom.";
					break;
				case 1041:
					cause = $"A tiki man pummeled {_configuration.SlotName} to death.";
					break;
				case 1043:
				case 1105:
					cause = $"{_configuration.SlotName}'s flesh is melting right off the bone.";
					break;
				case 1045:
				case 1049:
					cause = $"{_configuration.SlotName} was treated to a medieval welcome.";
					break;
				case 1100:
					cause = $"{_configuration.SlotName} fell on some spikes.";
					break;
				case 1101:
					cause = $"An arrow has pierced one of {_configuration.SlotName}'s vital organs.";
					break;
				case 1103:
				case 1110:
				case 1111:
				case 1206:
					cause = $"{_configuration.SlotName} has been ground into a fine paste.";
					break;
				case 1104:
					cause = $"{_configuration.SlotName} stepped too close to a tiki trap.";
					break;
				case 1112:
					cause = $"{_configuration.SlotName} has been burnt to a crisp.";
					break;
				case 1113:
				case 1200:
					cause = $"Something hard hit {_configuration.SlotName} in the head.";
					break;
				case 1201:
					cause = $"{_configuration.SlotName} broke every bone in my body.";
					break;
				case 1202:
					cause = $"{_configuration.SlotName} couldn't get away in time.";
					break;
				case 1207:
					cause = $"{_configuration.SlotName}'s frozen body shattered into a million pieces.";
					break;
				case 1208:
					cause = $"Help, {_configuration.SlotName} is still falling!";
					break;
				case 1214:
					cause = $"{_configuration.SlotName} offered themself to the Black Goddess.";
					break;
			}
			deathLinkService.SendDeathLink(new DeathLink(_configuration.SlotName, cause));
		}
	}

	UInt64 _frame = 0;
	public unsafe void MainLoop() {
		++_frame;
		// update archipelago messages
		var archipelago_messages_string = "";
		foreach (var message in _stringQueue) {
			if (_frame > message.timestamp + 150) {
				archipelago_messages_string += $"*";
			} else {
				archipelago_messages_string += $"{message.value}*";
			}
		}
		SetPinnableString(_testString, archipelago_messages_string, _testStringMaxLen);
	}

	[Function(CallingConventions.Stdcall)]
	public unsafe delegate void MainLoopHookDelegate();
	public IHook<MainLoopHookDelegate> MainLoopHook;
	public unsafe void MainHoopLoopImpl() {
		if (_mainLoopOnce) {
			_mainLoopOnce = false;
			MainLoopOnce();
		}

		MainLoop();

		MainLoopHook.OriginalFunction();
	}

	// int __usercall load_save@<eax>(GlobalState *a1@<esi>)
	[Function(Register.esi, Register.eax, StackCleanup.Callee)]
	public unsafe delegate int LoadSaveDelegate(GlobalState* a1);
	public IHook<LoadSaveDelegate> LoadSaveHook;
	public unsafe int LoadSaveImpl(GlobalState* globalState) {
		var result = LoadSaveHook.OriginalFunction(globalState);

		SetPinnableString(_killedBy, "Death Link", _killedByMaxLen);
		if (_global->killed_by_stats != _deathLinkMagicNumber) {
			PushKilledByHook.Disable();
		}

		return result;
	}

	[Function(Register.ebx, Register.eax, StackCleanup.Callee)]
	public unsafe delegate byte TryUnlockDelegate(int a1, JournalID id, JournalCategory category, byte a4);
	public IHook<TryUnlockDelegate> TryUnlockHook;
	public unsafe byte TryUnlock(int a1, JournalID id, JournalCategory category, byte a4) {
		var new_unlock = TryUnlockHook.OriginalFunction(a1, id, category, a4);

		if (!Enum.IsDefined(id)) {
			return new_unlock;
		}

		if (new_unlock != 0) {
			session.Locations.CompleteLocationChecks((int)id);
			// issue: can't display multiple messages at once
			var item = JournalCache.map[id];
			SetPinnableString(_tryUnlockString, $"{item.player}'s {item.name}", _tryUnlockStringMaxLen);
		}

		return new_unlock;
	}

	[Function(CallingConventions.MicrosoftThiscall)]
	public unsafe delegate int SpawnEntityDelegate(int @this, float x, float y, EntityType type, char active);
	public IHook<SpawnEntityDelegate> SpawnEntityHook;
	public unsafe int SpawnEntity(int @this, float x, float y, EntityType type, char active) {
		if (EntityTypesEnabled.disabled.Contains(type)) {
			return SpawnEntityHook.OriginalFunction(@this, x, y, EntityTypesEnabled.GetReplacementType(), active);
		}
		return SpawnEntityHook.OriginalFunction(@this, x, y, type, active);
	}

	[Function(Register.eax, Register.eax, StackCleanup.Callee)]
	public unsafe delegate int ResetSaveDelegate(GlobalState* a1, char a2);
	public IHook<ResetSaveDelegate> ResetSaveHook;
	public unsafe int ResetSave(GlobalState* globalState, char a2) {
		var result = ResetSaveHook.OriginalFunction(globalState, a2);

		globalState->done_tutorial = 1;

		return result;
	}

	#region Standard Overrides
	public override void ConfigurationUpdated(Config configuration)
	{
		// Apply settings from configuration.
		// ... your code here.
		_configuration = configuration;
		_logger.WriteLine($"[{_modConfig.ModId}] Config Updated: Applying");
	}
	#endregion

	#region For Exports, Serialization etc.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	public Mod() { }
#pragma warning restore CS8618
	#endregion
}
