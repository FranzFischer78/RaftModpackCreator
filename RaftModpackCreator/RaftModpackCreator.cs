using HarmonyLib;
using HMLLibrary;
using Newtonsoft.Json.Linq;
using RaftModLoader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;


public class RaftModpackCreator : Mod
{

	//TO DO: PUT ALL THE ASSETS IN ONE FILE!!!
	AssetBundle asset;
	AssetBundle assetModlistModitem;
	AssetBundle assetModeditorModitem;
	AssetBundle SetupModpackPanel;
	AssetBundle EditModpackPanel;
	AssetBundle MessageBoxBundle;
	AssetBundle hookedui;

	GameObject Canvas;
	GameObject menu;
	GameObject NewModpackPanel;
	GameObject EditModpackPanelGO;

	GameObject hookeduimenu;
	GameObject MessageBoxWindow;




	List<string> InstalledRmods = new List<string>();
	List<string> InstalledModNames = new List<string>();


	public class Rmod
	{
		public string Modname { get; set; }
		public string Path { get; set; }
	}

	List<Rmod> rmodModnameAssoc = new List<Rmod>();


	List<Modelem> allmodswithoutmodpackscache = new List<Modelem>();



	//Assembly variables
	List<string> customAssemblies = new List<string>();
	public List<Assembly> additional = new List<Assembly>();



	//Current Modpack Variables
	string CurrentModpackPath = "";
	string CurrentModpackName = "";
	List<string> CurrentModpackContent = new List<string>();
	bool CurrentModpackIncludeMods = true;


	List<string> Datafiles = new List<string>();

	bool Loaded;
	bool IsMenuOpen;

	HNotification notification;

	public static RaftModpackCreator instanceModpack;

	//Initialisation process: Patching + Assembly Loading
	public void Start()
	{
		Loaded = false;
		IsMenuOpen = false;
		instanceModpack = this;
		var harmony = new Harmony("com.franzfischer.modpacks");
		harmony.PatchAll();

		notification = FindObjectOfType<HNotify>().AddNotification(HNotify.NotificationType.spinning, "Loading Raft Modpack Creator...");


		customAssemblies.Add(@"Assemblies/" + "System.Buffers.dll");
		customAssemblies.Add(@"Assemblies/" + "System.Memory.dll");
		customAssemblies.Add(@"Assemblies/" + "System.IO.Compression.FileSystem.dll");
		customAssemblies.Add(@"Assemblies/" + "System.IO.Compression.ZipFile.dll");
		customAssemblies.Add(@"Assemblies/" + "System.IO.Compression.dll");


		foreach (var assemblyName in customAssemblies)
		{
			try
			{
				Debug.Log("Trying to load " + assemblyName);
				var bytes = GetEmbeddedFileBytes(assemblyName);

				var assembly = Assembly.Load(bytes);
				additional.Add(assembly);
			}
			catch (Exception e)
			{
				Debug.LogWarning(@"Couldn't load assembly because: " + e);
			}
		}

		AppDomain.CurrentDomain.AssemblyResolve += (x, y) =>
		{
			var name = new AssemblyName(y.Name).Name;
			foreach (var a in additional)
				if (a.GetName().Name == name)
					return a;
			return null;
		};

		StartCoroutine(InitFunc());



	}

	//Initialising stuff for assets and ui
	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
	public IEnumerator InitFunc()
	{
		//Loading AssetBundles
		AssetBundleCreateRequest request = AssetBundle.LoadFromMemoryAsync(GetEmbeddedFileBytes("Assets/modpack.assets"));
		yield return request;
		asset = request.assetBundle;
		AssetBundleCreateRequest request2 = AssetBundle.LoadFromMemoryAsync(GetEmbeddedFileBytes("Assets/modlistitem.assets"));
		yield return request2;
		assetModlistModitem = request2.assetBundle;
		AssetBundleCreateRequest request3 = AssetBundle.LoadFromMemoryAsync(GetEmbeddedFileBytes("Assets/modpackcreatorsetupmodpackpanel.assets"));
		yield return request3;
		SetupModpackPanel = request3.assetBundle;
		AssetBundleCreateRequest request4 = AssetBundle.LoadFromMemoryAsync(GetEmbeddedFileBytes("Assets/modeditoritem.assets"));
		yield return request4;
		assetModeditorModitem = request4.assetBundle;
		AssetBundleCreateRequest request5 = AssetBundle.LoadFromMemoryAsync(GetEmbeddedFileBytes("Assets/modpackcreatoreditmodpack.assets"));
		yield return request5;
		EditModpackPanel = request5.assetBundle;
		AssetBundleCreateRequest request6 = AssetBundle.LoadFromMemoryAsync(GetEmbeddedFileBytes("Assets/modpackcreatormessagebox.assets"));
		yield return request6;
		MessageBoxBundle = request6.assetBundle;
		AssetBundleCreateRequest request7 = AssetBundle.LoadFromMemoryAsync(GetEmbeddedFileBytes("Assets/modpacks_hookedui.assets"));
		yield return request7;
		hookedui = request7.assetBundle;
		Loaded = true;
		notification.Close();
		notification = FindObjectOfType<HNotify>().AddNotification(HNotify.NotificationType.normal, "Finished loading modpacks!", 5, HNotify.CheckSprite);

		//Debug.Log("Loading finished! Press f7 to use the modpack creator ;)");

		HookUI();

		//ADD REFS for Datafiles
		Datafiles.Add(@"ModPackCreatorData/" + "banner.jpg");
		Datafiles.Add(@"ModPackCreatorData/" + "data.txt");
		Datafiles.Add(@"ModPackCreatorData/" + "icon.png");
		Datafiles.Add(@"ModPackCreatorData/" + "modinfo.jsonfile");
		Datafiles.Add(@"ModPackCreatorData/" + "Modpacks.csfile");
		Datafiles.Add(@"ModPackCreatorData/" + "packages.config");
		Datafiles.Add(@"ModPackCreatorData/" + "modpackloader.assets");


	}

	//Reload UI HOOK AFTER LEAVING A world from a game
	/*public void WorldEvent_WorldUnloaded()
	{
		HookUI();
	}*/






	#region UIHOOK

	public List<string> EnabledMods = new List<string>();
	public bool LauchedPanelOneTime = false;
	public void HookUI()
	{
		GameObject MainMenuParent = GameObject.Find("MainMenuCanvas");

		GameObject MenuButtonsParent = MainMenuParent.transform.Find("MenuButtons").gameObject;

		GameObject NewGamePanelParent = MainMenuParent.transform.Find("New Game Box").gameObject;
		GameObject LoadGamePanelParent = MainMenuParent.transform.Find("Load Game Box").gameObject;

		GameObject CreateGameButton = MainMenuParent.transform.Find("New Game Box").transform.Find("CreateGameButton").gameObject;

		Debug.Log("Hooking UI");

		/*	try
			{
				//Hooking onto the main menu to add new buttons
				//Modpacks browser online
				GameObject ModpacksButton = Instantiate(MenuButtonsParent.transform.Find("New Game").gameObject, MenuButtonsParent.transform);
				ModpacksButton.transform.SetAsFirstSibling();
				Debug.Log("namebutton: " + ModpacksButton.name);
				ModpacksButton.GetComponentInChildren<Text>().text = "PUBLIC MODPACKS";
				ModpacksButton.GetComponent<Button>().onClick = new Button.ButtonClickedEvent();

				ModpacksButton.GetComponent<Button>().onClick.RemoveAllListeners();

				ModpacksButton.GetComponent<Button>().onClick.AddListener(() =>
				{
					Debug.Log("Modpack Browser");
					//LaunchModpackBrowser();
				});

				//Modpack creator
				ModpacksButton = Instantiate(MenuButtonsParent.transform.Find("New Game").gameObject, MenuButtonsParent.transform);
				ModpacksButton.transform.SetAsFirstSibling();
				Debug.Log("namebutton: " + ModpacksButton.name);
				ModpacksButton.GetComponentInChildren<Text>().text = "MODPACK CREATOR";
				ModpacksButton.GetComponent<Button>().onClick = new Button.ButtonClickedEvent();

				ModpacksButton.GetComponent<Button>().onClick.RemoveAllListeners();

				ModpacksButton.GetComponent<Button>().onClick.AddListener(() =>
				{
					OpenModpackCreatorWindow();
				});
			}
			catch (Exception e)
			{
				Debug.Log("Error adding button to main menu raft ui: " + e);
			}

			Debug.Log("run debugging");
			FindALLObjectsOfType<CanvasScaler>().ForEach(g => { Debug.Log("namgego : " + g.gameObject.name); });
			Debug.Log("finished run debugging");

			*/

		GameObject RmlMainMenuGO = FindObjectOfType<RaftModLoader.MainMenu>().gameObject;





		GameObject LeftNavBar = RmlMainMenuGO.transform.Find("BG").transform.Find("LeftBar").transform.Find("RMLMainMenu_Slots").gameObject;

		GameObject homeelemgo = LeftNavBar.transform.Find("RMLMainMenuLButton_Home").gameObject;


		GameObject ModpackButton = Instantiate(homeelemgo, LeftNavBar.transform);
		ModpackButton.name = "RMLMainMenuLButton_ModpackCreatePage";
		ModpackButton.GetComponentInChildren<TMPro.TMP_Text>().text = "MANAGE MODPACKS";
		ModpackButton.GetComponent<Button>().onClick.AddListener(() => { RaftModLoader.MainMenu.ChangeMenu("ModpackCreatePage"); RaftModpackCreatorPages.ModpackCreatePage.UpdateModpackSelector(GetExistingModpacks(), false); });
		GameObject ModpackCreatePageGO = Instantiate(hookedui.LoadAsset<GameObject>("ModpackCreatePage"), RaftModLoader.MainMenu.pages.transform);
		ModpackCreatePageGO.name = "ModpackCreatePage";
		RaftModLoader.MainMenu.menuPages.Add("ModpackCreatePage", ModpackCreatePageGO.AddComponent<RaftModpackCreatorPages.ModpackCreatePage>());
		ModpackCreatePageGO.SetActive(false);

		//Debug.Log("lol" + ModpackCreatePageGO.name);
		//Debug.Log(RaftModLoader.MainMenu.pages.transform.Find("ModpackCreatePage").gameObject.name + " lol");
		GameObject ModpackStoreButton = Instantiate(homeelemgo, LeftNavBar.transform);
		ModpackStoreButton.name = "RMLMainMenuLButton_ModpackBrowser";
		ModpackStoreButton.GetComponentInChildren<TMPro.TMP_Text>().text = "BROWSE MODPACKS (COMING SOON)";

		hookeduimenu = ModpackCreatePageGO;
		RaftModpackCreatorPages.ModpackCreatePage.ModpackCreator_ModpackChoose = ModpackCreatePageGO.transform.Find("ModpackCreator_ModpackChoose").gameObject;


		//INIT RMC UI
		GameObject ModpackContent = ModpackCreatePageGO.transform.Find("ModpackCreator_ModpackContent").gameObject;
		GameObject Modlist = ModpackCreatePageGO.transform.Find("ModpackCreator_Modslist").gameObject;

		allmodswithoutmodpackscache = GetAllModsExceptModpacks();

		GameObject ModlistContainer = Modlist.gameObject.transform.Find("Modlist_Viewport").gameObject.transform.Find("Modlist_Container").gameObject;
		GameObject prefab = assetModlistModitem.LoadAsset<GameObject>("ModpackCreator_Modslist_Moditem");

		allmodswithoutmodpackscache.ForEach(i =>
		{
			//Debug.Log("Adding to modpack " + i.name);
			GameObject ModListEntryElem = Instantiate(prefab, ModlistContainer.transform);

			ModListEntryElem.GetComponentInChildren<Text>().text = i.name;

			ModListEntryElem.GetComponent<Button>().onClick.AddListener(() => AddModToModpack(i.name, i.slug));

		});



		//init dropdown
		GameObject dropdown = RaftModpackCreatorPages.ModpackCreatePage.ModpackCreator_ModpackChoose;

		dropdown.GetComponent<Dropdown>().onValueChanged.AddListener((int index) =>
		{
			if (index == 0)
			{
				CurrentModpackContent.Clear();
				return;
			}

			try
			{
				LoadModpack(index - 1, ModpackContent);
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}
		});
		//init new button
		GameObject ModpackNewButton = ModpackCreatePageGO.transform.Find("ModpackCreator_NewMod").gameObject;
		ModpackNewButton.GetComponent<Button>().onClick.AddListener(NewModpack);

		//init save button
		GameObject ModpackSaveButton = ModpackCreatePageGO.transform.Find("ModpackCreator_SaveMod").gameObject;
		ModpackSaveButton.GetComponent<Button>().onClick.AddListener(SaveModpack);

		//init include mods checkbox
		GameObject ModpackIncludeModsCheckBox = ModpackCreatePageGO.transform.Find("ModpackCreator_IncludeModsCheck").gameObject;
		ModpackIncludeModsCheckBox.GetComponent<Toggle>().onValueChanged.AddListener((bool lol) =>
		{
			if (!CurrentModpackName.IsNullOrEmpty())
			{
				CurrentModpackIncludeMods = lol;
			}
			else
			{
				ModpackIncludeModsCheckBox.GetComponent<Toggle>().isOn = !lol;
			}

		});
		RaftModpackCreatorPages.ModpackCreatePage.ModpackCreator_includemodtoggle = ModpackIncludeModsCheckBox;

		/*Debug.Log("Running debug check");
		foreach (Transform transformn in MenuButtonsParent.GetComponentsInChildren<Transform>())
		{

			Debug.Log("name: " + transformn.gameObject.name);

		}*/




		//CreateMessageBoxWin();

		//--- TEMPORARELY DISABLE MOD PROFILES ---
		return;
		//--- REMOVE TO REENABLE THE FUNCTIONALITY. PERM UNLOADING DOES NOT WORK PROPERLY. ENABLE AT YOUR OWN RISK!!! ---

		#region MOD PROFILES => DISABLED!!!
		//Instantiating the selection panel
		try
		{
			GameObject toInstantiate = hookedui.LoadAsset<GameObject>("ModpackWorldSelect");
			//Debug.Log("Got GameObject");
			GameObject lol = Instantiate(toInstantiate, NewGamePanelParent.transform);
			lol.GetComponent<Button>().onClick.AddListener(() => InitWorldModSelector(true));
		}
		catch (NullReferenceException e)
		{
			Debug.Log("Couldn't instantiate on Ui " + e);
		}

		try
		{
			GameObject toInstantiate = hookedui.LoadAsset<GameObject>("ModpackWorldSelect");
			//Debug.Log("Got GameObject");
			GameObject lol = Instantiate(toInstantiate, LoadGamePanelParent.transform);
			lol.GetComponent<Button>().onClick.AddListener(() => InitWorldModSelector(false));
		}
		catch (NullReferenceException e)
		{
			Debug.Log("Couldn't instantiate on Ui " + e);
		}


		//Patching buttons
		//Debug.Log("Button patch");

		GameObject NewWorldButton = MenuButtonsParent.transform.Find("New Game").gameObject;
		//Debug.Log("Button patch1");

		GameObject LoadWorldButton = MenuButtonsParent.transform.Find("Load game").gameObject;
		//Debug.Log("Button patch2");

		NewWorldButton.GetComponent<Button>().onClick.AddListener(() => { Debug.Log("Opened new world Menu"); EnabledMods.Clear(); });
		LoadWorldButton.GetComponent<Button>().onClick.AddListener(() =>
		{
			LauchedPanelOneTime = false;

			Debug.Log("Opened load world Menu");
			EnabledMods.Clear();

			ReloadMods();

		});

		#endregion

	}


	public static List<T> FindALLObjectsOfType<T>()
	{
		return UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()
			.SelectMany(g => g.GetComponentsInChildren<T>(true))
			.ToList();
	}

	public void ReloadMods()
	{
		LoadGame_Selection[] gameFiles = FindObjectsOfType<LoadGame_Selection>();

		List<string> excludes = new List<string>();
		excludes.Add("Modpacks");
		excludes.Add("ModUpdater");
		excludes.Add("ModUtils");
		excludes.Add("Extra Settings API");
		List<string> allMods = new List<string>();


		HMLLibrary.ModManagerPage.modList.ForEach(item =>
		{

			string name = item.jsonmodinfo.name;

			if (!excludes.Contains(name))
			{
				allMods.Add(name);

			}





		}
		);



		foreach (LoadGame_Selection game_Selection in gameFiles)
		{
			GameObject elem = game_Selection.gameObject;

			//Listener start
			elem.GetComponent<Button>().onClick.AddListener(() =>
			{



				LoadGame_Selection[] gameFiless = FindObjectsOfType<LoadGame_Selection>();
				string worldnamee = "";

				foreach (LoadGame_Selection game_Selectionn in gameFiless)
				{
					GameObject elemm = game_Selectionn.gameObject;
					string namee = elemm.transform.Find("GameName").gameObject.GetComponent<Text>().text;


					if (elemm.transform.Find("SelectionBG").gameObject.activeSelf)
					{
						worldnamee = namee;
						break;
					}
				}

				if (File.Exists(SaveAndLoad.WorldPath + worldnamee + @"\modprofile.txt"))
				{
					string[] modprofile = System.IO.File.ReadAllLines(SaveAndLoad.WorldPath + worldnamee + @"\modprofile.txt");

					EnabledMods.Clear();
					for (int i = 0; i < modprofile.Length; i++)
					{

						EnabledMods.Add(modprofile[i]);
					}

					foreach (string mod in allMods)
					{
						if (excludes.Contains(mod))
						{
							continue;
						}


						if (EnabledMods.Contains(mod))
						{
							DefaultConsoleCommands.ModLoad(new string[] { mod });
						}
						else
						{
							ForceUnloadMod(mod);
							DefaultConsoleCommands.ModUnload(new string[] { mod });
						}
					}
				}
			});
			//listener end


		}

	}

	public void InitWorldModSelector(bool NewWorld)
	{
		GameObject MainMenuParent = GameObject.Find("MainMenuCanvas");

		List<string> excludes = new List<string>();
		excludes.Add("Modpacks");
		excludes.Add("ModUpdater");
		excludes.Add("ModUtils");
		excludes.Add("Extra Settings API");



		//Debug.Log("Init world selector");
		GameObject ModSelector;
		if (NewWorld)
		{
			ModSelector = Instantiate(hookedui.LoadAsset<GameObject>("Modpack_ModSelectorWorld"), GameObject.Find("MainMenuCanvas").transform.Find("New Game Box").gameObject.transform);

		}
		else
		{
			ModSelector = Instantiate(hookedui.LoadAsset<GameObject>("Modpack_ModSelectorWorld"), GameObject.Find("MainMenuCanvas").transform.Find("Load Game Box").gameObject.transform);

		}
		List<string> allMods = new List<string>();

		if (!NewWorld)
		{

			//Get data from old world
			LoadGame_Selection[] gameFiles = FindObjectsOfType<LoadGame_Selection>();
			string worldname = "";

			foreach (LoadGame_Selection game_Selection in gameFiles)
			{
				GameObject elem = game_Selection.gameObject;
				string name = elem.transform.Find("GameName").gameObject.GetComponent<Text>().text;
				if (elem.transform.Find("SelectionBG").gameObject.activeSelf)
				{
					worldname = name;
					break;
				}
			}

			if (worldname.IsNullOrEmpty())
			{
				Destroy(ModSelector);
				FindObjectOfType<HNotify>().AddNotification(HNotify.NotificationType.normal, "Select a world first!", 3, HNotify.ErrorSprite);
				return;
			}

			if (File.Exists(SaveAndLoad.WorldPath + worldname + @"\modprofile.txt"))
			{
				string[] modprofile = System.IO.File.ReadAllLines(SaveAndLoad.WorldPath + worldname + @"\modprofile.txt");

				EnabledMods.Clear();
				for (int i = 0; i < modprofile.Length; i++)
				{

					EnabledMods.Add(modprofile[i]);
				}

				foreach (string mod in allMods)
				{
					if (excludes.Contains(mod))
					{
						continue;
					}


					if (EnabledMods.Contains(mod))
					{
						DefaultConsoleCommands.ModLoad(new string[] { mod });
					}
					else
					{
						ForceUnloadMod(mod);

						DefaultConsoleCommands.ModUnload(new string[] { mod });
					}
				}
			}

		}



		HMLLibrary.ModManagerPage.modList.ForEach(item =>
		{

			string name = item.jsonmodinfo.name;

			if (!excludes.Contains(name))
			{
				allMods.Add(name);

			}





		}
		);



		foreach (string mod in allMods)
		{
			GameObject elem = Instantiate(hookedui.LoadAsset<GameObject>("Modpack_ModSelectorItem"), ModSelector.transform.Find("ModsSelector").transform.Find("Viewport").Find("Content").transform);
			elem.transform.Find("ToggleMod").Find("LabelMod").gameObject.GetComponent<Text>().text = mod;

			if (EnabledMods.Count != 0)
			{
				if (EnabledMods.Contains(mod))
				{
					elem.transform.Find("ToggleMod").gameObject.GetComponent<Toggle>().isOn = true;
				}
			}


			elem.transform.Find("ToggleMod").gameObject.GetComponent<Toggle>().onValueChanged.AddListener((bool state) =>
			{
				if (state)
					EnabledMods.Add(mod);
				else
					EnabledMods.Remove(mod);
			});
		}

		ModSelector.transform.Find("ApplyMods").gameObject.GetComponent<Button>().onClick.AddListener(() =>
		{
			//Force reload
			//Force unload assetBundles



			foreach (string mod in allMods)
			{
				if (excludes.Contains(mod))
				{
					continue;
				}


				if (EnabledMods.Contains(mod))
				{
					DefaultConsoleCommands.ModLoad(new string[] { mod });
				}
				else
				{
					ForceUnloadMod(mod);
					DefaultConsoleCommands.ModUnload(new string[] { mod });
				}
			}
			Destroy(ModSelector);

		});


		GameObject CreateGameButton = MainMenuParent.transform.Find("New Game Box").transform.Find("CreateGameButton").gameObject;
		CreateGameButton.GetComponent<Button>().onClick.AddListener(() =>
		{
			Debug.Log("[MODPACKS] Launch create game");
			//Debug.Log(SaveAndLoad.WorldPath + SaveAndLoad.CurrentGameFileName);
			string Modlist = "";

			foreach (string mod in EnabledMods)
			{
				Modlist += mod + "\n";
			}

			Directory.CreateDirectory(SaveAndLoad.WorldPath + SaveAndLoad.CurrentGameFileName);
			System.IO.File.WriteAllText(SaveAndLoad.WorldPath + SaveAndLoad.CurrentGameFileName + @"\modprofile.txt", Modlist);



		});





		GameObject LoadGameButton = MainMenuParent.transform.Find("Load Game Box").transform.Find("LoadGameButton").gameObject;
		LoadGameButton.GetComponent<Button>().onClick.AddListener(() =>
			{
				Debug.Log("[MODPACKS] Launch load game");
				//Debug.Log(SaveAndLoad.WorldPath + SaveAndLoad.CurrentGameFileName);
				string Modlist = "";

				foreach (string mod in EnabledMods)
				{
					Modlist += mod + "\n";
				}
				LoadGame_Selection[] gameFiles = FindObjectsOfType<LoadGame_Selection>();

				string worldname = "";

				foreach (LoadGame_Selection game_Selection in gameFiles)
				{
					GameObject elem = game_Selection.gameObject;
					string name = elem.transform.Find("GameName").gameObject.GetComponent<Text>().text;
					if (elem.transform.Find("SelectionBG").gameObject.activeSelf)
					{
						worldname = name;
						break;
					}
				}

				//Directory.CreateDirectory(SaveAndLoad.WorldPath + SaveAndLoad.CurrentGameFileName);
				System.IO.File.WriteAllText(SaveAndLoad.WorldPath + worldname + @"\modprofile.txt", Modlist);



			});

		GameObject CancelButton = ModSelector.transform.Find("CancelButton").gameObject;

		CancelButton.GetComponent<Button>().onClick.AddListener(() =>
		{
			Destroy(ModSelector);
		}
		);



	}




	#endregion


	#region ForceLoad_Unload

	public void ForceUnloadMod(string mod)
	{
		Debug.Log("Check for force unload");

		List<ModData> modlist = HMLLibrary.ModManagerPage.modList;

		Dictionary<string, byte[]> modfiledict = new Dictionary<string, byte[]>();

		foreach (ModData md in modlist)
		{
			if (md.jsonmodinfo.name == mod)
			{
				modfiledict = md.modinfo.modFiles;
			}
		}
		if (modfiledict.Count == 0)
		{
			Debug.Log("[MODPACKS] An unexpected error happened while trying to force unload");
			return;
		}
		else
		{
			foreach (KeyValuePair<string, byte[]> keyValuePair in modfiledict)
			{
				if (keyValuePair.Key.Contains(".asset"))
				{
					Debug.Log("Unloading " + keyValuePair.Key);
					EnsureBundleUnload(keyValuePair.Key);
				}
			}
		}
	}

	public static void EnsureBundleUnload(string name)
	{
		AssetBundle.GetAllLoadedAssetBundles().ToList().ForEach(assetbundle =>
		{

			if (!assetbundle.name.Contains(".assets"))
			{
				name = name.Split('.')[0];
			}
			Debug.Log(assetbundle.name);
			Debug.Log(name);


			if (assetbundle.name == name)
			{
				assetbundle.Unload(true);
				Debug.Log("Unloaded " + name);
			}
		});
	}


	#endregion





	#region RMCBUTTONS
	public void NewModpack()
	{

		//Instantiate Modpack Panel and assign Functions
		NewModpackPanel = Instantiate(hookedui.LoadAsset<GameObject>("ModpackCreator_SetupModpackPanel"), FindObjectOfType<RaftModpackCreatorPages.ModpackCreatePage>().gameObject.transform);
		//Idk if useful but should prevent if raft has several main menu scenes

		DontDestroyOnLoad(NewModpackPanel);

		NewModpackPanel.transform.Find("SetupModpack_CreateModpack").GetComponent<Button>().onClick.AddListener(CreateNewModpack);
		NewModpackPanel.transform.Find("SetupModpack_ClosePanel").GetComponent<Button>().onClick.AddListener(CloseModpackPanel);




	}





	#endregion


	#region NewModpackPanelFunctions
	public void CreateNewModpack()
	{
		string ModpackInputName = NewModpackPanel.transform.Find("SetupModpack_InputName").gameObject.transform.Find("Text").gameObject.GetComponent<Text>().text;
		//Banner and Icon will follow later: not that important. Perhaps for the modpacks "store"


		if (!ModpackInputName.IsNullOrEmpty())
		{
			GameObject ModlistContainer = hookeduimenu.transform.Find("ModpackCreator_ModpackContent").gameObject.transform.Find("ModpackContent_Viewport").gameObject.transform.Find("ModpackContent_Container").gameObject;

			foreach (Transform child in ModlistContainer.transform)
			{
				//I need other variable names ;D
				Destroy(child.gameObject);

			}

			//Clean the cache
			CurrentModpackContent.Clear();
			//Set current vars
			CurrentModpackName = ModpackInputName;
			ModpackInputName = ModpackInputName.Replace(" ", "-");
			CurrentModpackPath = @"mods\" + ModpackInputName + ".rmod";

			//Debug.Log(CurrentModpackName);
			//Debug.Log(CurrentModpackPath);
			var allmodpacks = GetExistingModpacks();
			allmodpacks.Add(CurrentModpackName);
			//Debug.Log("almmodpaacks count " + allmodpacks.Count);

			RaftModpackCreatorPages.ModpackCreatePage.UpdateModpackSelector(allmodpacks, true);
			//RaftModpackCreatorPages.ModpackCreatePage.ModpackCreator_ModpackChoose.GetComponent<Dropdown>().value = (int)(allmodpacks.Count);

			Destroy(NewModpackPanel);
		}
		else
		{
			CreateMessageBox("Failed to create Modpack", "You must specify a name for the Modpack!");
		}
	}

	public void CloseModpackPanel()
	{
		Destroy(NewModpackPanel);
	}
	#endregion


	#region Saveandloadmodpacks
	public void SaveModpack()
	{
		if (CurrentModpackName != null && CurrentModpackPath != null)
		{
			//Save Modpack
			if (File.Exists(CurrentModpackPath))
			{
				//Update Contents
				string temppath = Path.GetTempPath() + @"\modpackcreator\";
				if (Directory.Exists(temppath))
				{
					Directory.Delete(temppath, true);
				}

				Directory.CreateDirectory(temppath);

				System.IO.Compression.ZipFile.ExtractToDirectory(CurrentModpackPath, temppath);

				File.WriteAllText(temppath + "data.txt", GetModlistItems());

				string JsonModinfoContent = File.ReadAllText(temppath + "modinfo.json");

				JObject json = JObject.Parse(JsonModinfoContent);
				json["name"] = (string)CurrentModpackName;
				json["description"] = (string)json["description"] + (string)CurrentModpackName;

				File.WriteAllText(temppath + "modinfo.json", json.ToString());

				if (CurrentModpackIncludeMods == true)
				{
					CopyRmodsToModpack(temppath);
				}

				File.Delete(CurrentModpackPath);
				System.IO.Compression.ZipFile.CreateFromDirectory(temppath, CurrentModpackPath);

				Directory.Delete(temppath, true);
				CreateMessageBox("Saved", "Succesfully edited modpack");

			}
			else
			{
				string temppath = Path.GetTempPath() + @"\modpackcreator\";





				if (Directory.Exists(temppath))
				{
					Directory.Delete(temppath, true);
				}

				Directory.CreateDirectory(temppath);


				foreach (string datafile in Datafiles)
				{
					var bytes = GetEmbeddedFileBytes(datafile);

					if (datafile == (@"ModPackCreatorData/" + "Modpacks.csfile"))
					{
						File.WriteAllBytes(temppath + Path.GetFileNameWithoutExtension(datafile) + ".cs", bytes);
					}
					else if (datafile == (@"ModPackCreatorData/" + "modinfo.jsonfile"))
					{
						File.WriteAllBytes(temppath + Path.GetFileNameWithoutExtension(datafile) + ".json", bytes);
					}
					else
					{
						File.WriteAllBytes(temppath + Path.GetFileName(datafile), bytes);
					}

				}

				File.WriteAllText(temppath + "data.txt", GetModlistItems());

				string JsonModinfoContent = File.ReadAllText(temppath + "modinfo.json");

				JObject json = JObject.Parse(JsonModinfoContent);
				json["name"] = (string)CurrentModpackName;
				json["description"] = (string)json["description"] + (string)CurrentModpackName;

				File.WriteAllText(temppath + "modinfo.json", json.ToString());

				if (CurrentModpackIncludeMods == true)
				{
					CopyRmodsToModpack(temppath);
				}

				System.IO.Compression.ZipFile.CreateFromDirectory(temppath, CurrentModpackPath);

				Directory.Delete(temppath, true);

				CreateMessageBox("Saved", "Succesfully created a new modpack");

			}

		}
		else
		{
			CreateMessageBox("Error", "Create a modpack first");
			return;
		}
	}

	public void LoadModpack(int modpackindex, GameObject modpackcontentlist)
	{
		CurrentModpackContent = new List<string>();

		try
		{
			var modpacktoload = GetExistingModpacksWithFilename()[modpackindex];
			ModData modpackmoddata = new ModData();

			HMLLibrary.ModManagerPage.modList.ForEach(i =>
			{
				if (i.jsonmodinfo.name == modpacktoload.name)
				{
					modpackmoddata = i;
				}
			}
			);


			try
			{

				if (modpackmoddata == null)
				{
					Debug.Log("The modpack could not be found!");
					CreateMessageBox("Unexpected!", "The modpack could not be found!");
					return;
				}

				if (!modpackmoddata.modinfo.modFiles.ContainsKey("data.txt"))
				{
					Debug.Log("The specified file is not a modpack!");
					CreateMessageBox("Unexpected!", "The specified file is not a modpack!");
					return;
				}
			}
			catch (Exception e)
			{
				CreateMessageBox("Unexpected!", "Moddata does not contain the nescessary information!");
				Debug.Log("Unexpected moddata error: " + e);
				/*Debug.Log(modpackmoddata.modinfo.modFiles.Keys.Count);

				foreach(string key in modpackmoddata.modinfo.modFiles.Keys)
				{
					Debug.Log("Key: " + key);
				}
				return;*/
			}


			GameObject ModlistContainer = modpackcontentlist.transform.Find("ModpackContent_Viewport").gameObject.transform.Find("ModpackContent_Container").gameObject;

			foreach (Transform child in ModlistContainer.transform)
			{
				//I need other variable names ;D
				Destroy(child.gameObject);

			}


			var DataTxtContent = "";
			try
			{
				var bytes = modpackmoddata.modinfo.modFiles["data.txt"];
				DataTxtContent = System.Text.Encoding.UTF8.GetString(bytes);
			}
			catch (Exception e)
			{
				CreateMessageBox("Unexpected!", "Moddata does not contain the nescessary information!");
				Debug.Log("Unexpected moddata error: " + e);
				/*Debug.Log(modpackmoddata.modinfo.modFiles.Keys.Count);

				foreach (string key in modpackmoddata.modinfo.modFiles.Keys)
				{
					Debug.Log("Key: " + key);
				}
				return;*/
			}

			if (DataTxtContent.IsNullOrEmpty())
			{
				Debug.Log("data.txt does not exist or is empty!");
				CreateMessageBox("Unexpected!", "data.txt does not exist or is empty!");
				return;
			}

			List<string> mods = new List<string>();

			using (StringReader reader = new StringReader(DataTxtContent))
			{
				string line = string.Empty;
				do
				{
					line = reader.ReadLine();
					if (line != null)
					{
						mods.Add(line);
					}

				} while (line != null);
			}


			string[] modsarray = mods.ToArray();

			CurrentModpackContent.Clear();

			foreach (string moditem in modsarray)
			{
				Debug.Log("moditem" + moditem);
				AddModToModpack(FindNameBySlug(moditem), moditem);
			}

			//Read rmoddatatxt
			var RmodDataTxtContent = "";
			try
			{
				var bytes = modpackmoddata.modinfo.modFiles["rmoddata.txt"];
				RmodDataTxtContent = System.Text.Encoding.UTF8.GetString(bytes);
			}
			catch (Exception e)
			{
				CreateMessageBox("Unexpected!", "Moddata does not contain the nescessary information!");
				Debug.Log("Unexpected moddata error: " + e);
				/*Debug.Log(modpackmoddata.modinfo.modFiles.Keys.Count);

				foreach (string key in modpackmoddata.modinfo.modFiles.Keys)
				{
					Debug.Log("Key: " + key);
				}
				return;*/
			}

			if (RmodDataTxtContent.IsNullOrEmpty())
			{
				Debug.Log("rmoddata.txt does not exist or is empty! Proceed without => REMOTE MODPACK");
				CurrentModpackIncludeMods = true;

			}
			else
			{

				List<string> rmods = new List<string>();

				using (StringReader reader = new StringReader(RmodDataTxtContent))
				{
					string line = string.Empty;
					do
					{
						line = reader.ReadLine();
						if (line != null)
						{
							rmods.Add(line);
						}

					} while (line != null);
				}

				string[] rmodsarray = rmods.ToArray();




				CurrentModpackIncludeMods = true;

			}


			CurrentModpackName = modpacktoload.name;
			CurrentModpackPath = modpacktoload.filename;

			RaftModpackCreatorPages.ModpackCreatePage.ModpackCreator_includemodtoggle.GetComponent<Toggle>().isOn = CurrentModpackIncludeMods;
			RaftModpackCreatorPages.ModpackCreatePage.UpdateModpackSelector(GetExistingModpacks(), false);

		}
		catch (Exception e)
		{
			Debug.Log("Failed to load Modpack with Exception: " + e);
		}

	}

	#endregion

	public static List<Modpack> GetExistingModpacksWithFilename()
	{

		List<Modpack> modpacks = new List<Modpack>();

		HMLLibrary.ModManagerPage.modList.ForEach(i =>
		{

			if (i.modinfo.modFiles.ContainsKey("data.txt"))
			{
				Modpack modpack = new Modpack();

				modpack.filename = i.modinfo.modFile.FullName;
				modpack.name = i.jsonmodinfo.name;

				modpacks.Add(modpack);

				//Debug.Log("file is a modpack" + modpack.filename + "    " + modpack.name);
			}
			else
			{
				//Debug.Log("The file is not a modpack");
			}

		}
		);

		return modpacks;


	}

	public static List<Modelem> GetAllModsExceptModpacks()
	{
		var tmp = GetExistingModpacks();
		var allmods = HMLLibrary.ModManagerPage.modList;

		List<Modelem> modelems = new List<Modelem>();

		allmods.ForEach(i =>
		{
			if (!tmp.Contains(i.jsonmodinfo.name))
			{
				Modelem modelem = new Modelem();
				modelem.name = i.jsonmodinfo.name;
				modelem.slug = i.jsonmodinfo.updateUrl.Split('/')[6];
				modelem.filename = i.modinfo.modFile.Name;
				modelems.Add(modelem);
			}
		});

		return modelems;


	}


	public static List<string> GetExistingModpacks()
	{
		List<string> modpacks = new List<string>();
		List<Modpack> modpackstmp = new List<Modpack>();

		modpackstmp = GetExistingModpacksWithFilename();
		modpackstmp.ForEach(i => modpacks.Add(i.name));

		return modpacks;


	}

	public static string FindSlugByName(string name)
	{
		var tmp = HMLLibrary.ModManagerPage.modList;

		string output = "";

		tmp.ForEach(i =>
		{

			if (i.jsonmodinfo.name == name)
			{
				output = i.jsonmodinfo.updateUrl.Split('/')[6];
			}

		});

		return output;
	}

	public static string FindNameBySlug(string slug)
	{
		var tmp = HMLLibrary.ModManagerPage.modList;

		string output = "";

		tmp.ForEach(i =>
		{

			if (i.jsonmodinfo.updateUrl.Split('/')[6] == slug)
			{
				output = i.jsonmodinfo.name;
			}

		});

		Debug.Log(output);

		return output;
	}

	public static string FindPathBySlug(string slug)
	{
		var tmp = HMLLibrary.ModManagerPage.modList;

		string output = "";

		tmp.ForEach(i =>
		{

			if (i.jsonmodinfo.updateUrl.Split('/')[6] == slug)
			{
				output = i.modinfo.modFile.Name;
			}

		});

		return output;
	}

	public string GetModlistItems()
	{
		string Items = "";

		//GameObject ModlistContainer = menu.transform.Find("ModpackCreator_ModpackContent").gameObject.transform.Find("ModpackContent_Viewport").gameObject.transform.Find("ModpackContent_Container").gameObject;



		foreach (string elem in CurrentModpackContent)
		{
			Items += elem + "\n";

		}


		return Items;
	}

	//--- WEIRD FUNCTION
	/*public string[] GetModlistItemsArray()
	{
		List<string> Items = new List<string>();

		GameObject ModlistContainer = menu.transform.Find("ModpackCreator_ModpackContent").gameObject.transform.Find("ModpackContent_Viewport").gameObject.transform.Find("ModpackContent_Container").gameObject;





		foreach (Transform child in ModlistContainer.transform)
		{
			string textelem = child.transform.Find("ModpackCreator_Modeditor_ModitemElem").gameObject.transform.Find("Text").gameObject.GetComponent<Text>().text;
			Items.Add(textelem);

		}


		return Items.ToArray();
	}*/

	public void CopyRmodsToModpack(string Temppath)
	{
		string destination = Temppath + @"Mods\";

		//var ModlistItemsArr = GetModlistItemsArray();


		/*Debug.Log(InstalledRmods);
		Debug.Log(InstalledModNames);

		for(int i = 0; i < ModlistItemsArr.Length; i++)
		{
			var path = InstalledRmods[InstalledModNames.IndexOf(ModlistItemsArr[i])];

			File.Copy(path, destination + Path.GetFileName(path), true);
		}*/

		//The following is def. not the way to go. Real cpu intensive
		//Will patch that as soon as possible
		/*var rmodFiles = Directory.EnumerateFiles(@"mods\");
		List<string> rmodModNames = new List<string>();


		foreach (object rmod in rmodFiles)
		{

			if (Path.GetExtension(rmod.ToString()) == ".rmod")
			{


				//Read rmod
				string ModInfo = "";

				try
				{

					using (System.IO.Compression.ZipArchive archive = System.IO.Compression.ZipFile.Open(rmod.ToString(), System.IO.Compression.ZipArchiveMode.Read))
					{
						System.IO.Compression.ZipArchiveEntry entry = archive.GetEntry("modinfo.json");

						using (StreamReader sr = new StreamReader(entry.Open()))
						{
							ModInfo = sr.ReadToEnd();

							JObject ModinfoJson = JObject.Parse(ModInfo);
							//Debug.Log(ModinfoJson["name"].ToString());
							rmodModNames.Add(ModinfoJson["name"].ToString());

						}

					}
				}
				catch (Exception e)
				{
					Debug.LogWarning("Zip Error: " + e);
				}
			}
		}

		Directory.CreateDirectory(destination);

		List<string> rmodFilesUsedArrr = new List<string>();

		for (int i = 0; i < ModlistItemsArr.Length; i++)
		{
			var rmodNamesArr = rmodModNames.ToArray();
			var item = ModlistItemsArr[i];
			var rmodFilesArr = rmodFiles.ToArray();
			var path = rmodFilesArr[rmodModNames.IndexOf(item)];
			rmodFilesUsedArrr.Add(path);
			File.Copy(path, destination + Path.GetFileName(path), true);
		}*/




		//Wrtie rmod data file
		string Items = "";



		foreach (var rmodFile in CurrentModpackContent)
		{
			var path = FindPathBySlug(rmodFile);

			Items += Path.GetFileName(path) + "\n";
		}


		File.WriteAllText(Temppath + "rmoddata.txt", Items);


	}




	#region Add/Remove to/from Modpack
	public void AddModToModpack(string Modname, string slug)
	{
		if (CurrentModpackContent.Contains(slug))
		{
			CreateMessageBox("Don't spam :)", "Can't have a mod twice in a modpack");
			return;
		}

		CurrentModpackContent.Add(slug);

		try
		{

			GameObject ModeditorContainer = FindObjectOfType<RaftModpackCreatorPages.ModpackCreatePage>().gameObject.transform.Find("ModpackCreator_ModpackContent").gameObject.transform.Find("ModpackContent_Viewport").gameObject.transform.Find("ModpackContent_Container").gameObject;
			GameObject prefab = assetModeditorModitem.LoadAsset<GameObject>("ModpackCreator_Modeditor_Moditem");

			GameObject ModeditorEntryElem = Instantiate(prefab, ModeditorContainer.transform);

			ModeditorEntryElem.transform.Find("ModpackCreator_Modeditor_ModitemElem").gameObject.transform.Find("Text").gameObject.GetComponent<Text>().text = Modname;

			//ModeditorEntryElem.transform.Find("Modeditor_DelElemButton").gameObject.GetComponent<Button>().onClick.AddListener(() => RemoveFromModpack(Modname));
			ModeditorEntryElem.transform.Find("ModpackCreator_Modeditor_ModitemElem").gameObject.GetComponent<Button>().onClick.AddListener(() => RemoveFromModpack(Modname, slug));

		}
		catch (Exception e)
		{
			Debug.LogException(e);
		}
	}

	public void RemoveFromModpack(string Modname, string slug)
	{
		GameObject ModlistContainer = hookeduimenu.transform.Find("ModpackCreator_ModpackContent").gameObject.transform.Find("ModpackContent_Viewport").gameObject.transform.Find("ModpackContent_Container").gameObject;

		foreach (Transform child in ModlistContainer.transform)
		{
			string textelem = child.transform.Find("ModpackCreator_Modeditor_ModitemElem").gameObject.transform.Find("Text").gameObject.GetComponent<Text>().text;
			if (textelem == Modname)
			{
				//I need other variable names ;D
				Destroy(child.gameObject);

			}

		}

		CurrentModpackContent.Remove(FindSlugByName(slug));
	}

	#endregion


	#region MessageBox
	public void CreateMessageBoxWin()
	{
		Debug.Log("passed inst");
		DontDestroyOnLoad(MessageBoxWindow);
		MessageBoxWindow.SetActive(false);

	}

	public void CreateMessageBox(string Title, string Content)
	{
		//Debug.Log(Title + " : " + Content);
		MessageBoxWindow = Instantiate(hookedui.LoadAsset<GameObject>("ModpackCreator_MessageBox"), hookeduimenu.transform);
		MessageBoxWindow.transform.Find("ModpackCreator_MessageBoxClose").GetComponent<Button>().onClick.AddListener(CloseMessageBox);

		MessageBoxWindow.transform.Find("ModpackCreator_MessageBoxTitle").GetComponent<Text>().text = Title;
		MessageBoxWindow.transform.Find("ModpackCreator_MessageBoxContent").GetComponent<Text>().text = Content;

	}
	public void CloseMessageBox()
	{
		Destroy(MessageBoxWindow);
	}
	#endregion


	public void OnModUnload()
	{
		asset.Unload(true);
		Debug.Log("Mod RaftModpackCreator has been unloaded!");
	}
}


#region Harmony


//Reload UI HOOK PATCHES
#region HarmonyUIHOOK

[HarmonyPatch]
static class Patch_Coroutine_UIHOOK_SceneLoad
{
	static MethodBase TargetMethod() => typeof(Raft).Assembly.GetTypes().First(x => x.FullName.StartsWith("LoadSceneManager+<Load>")).GetMethod("MoveNext", ~BindingFlags.Default);
	static void Postfix(ref bool __result, ref string ___sceneName)
	{
		if (!__result)
		{
			Debug.Log("[MODPACKS] Load main menu postfix " + ___sceneName);
			if (___sceneName == "MainMenuScene")
			{
				Debug.Log("[MODPACKS] Here we go again: HOOKING UI");
				//RaftModpackCreator.instanceModpack.HookUI();
			}
		}
	}
}
#endregion








#endregion

public class Modpack
{
	public string name;
	public string filename;

}

public class Modelem
{
	public string name;
	public string filename;
	public string slug;

}