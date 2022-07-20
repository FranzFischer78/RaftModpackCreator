using Newtonsoft.Json.Linq;
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
	GameObject MessageBoxWindow;






	List<string> InstalledRmods = new List<string>();
	List<string> InstalledModNames = new List<string>();


	public class Rmod
	{
		public string Modname { get; set; }
		public string Path { get; set; }
	}

	List<Rmod> rmodModnameAssoc = new List<Rmod>();






	List<string> customAssemblies = new List<string>();

	//public IDictionary<string, Assembly> additional = new Dictionary<string, Assembly>();
	public List<Assembly> additional = new List<Assembly>();



	//Current Modpack Variables
	string CurrentModpackPath = "";
	string CurrentModpackName = "";


	List<string> Datafiles = new List<string>();

	bool Loaded;
	bool IsMenuOpen;

	HNotification notification;


	public void Start()
	{
		Loaded = false;
		IsMenuOpen = false;

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

	public void Update()
	{
		if (Loaded)
		{
			if (Input.GetKeyDown(KeyCode.F7))
			{
				if (RAPI.IsCurrentSceneMainMenu() && IsMenuOpen == false)
				{
					if (GameObject.Find("ModpackCreator_Canvas") != null)
					{
						LoadMainMenu(false);
					}
					if (GameObject.Find("ModpackCreator_Canvas") == null)
					{
						LoadMainMenu(true);
					}
				}
			}
		}


	}





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
		notification = FindObjectOfType<HNotify>().AddNotification(HNotify.NotificationType.normal, "Loading finished! Press f7 to use the modpack creator ;)", 5, HNotify.CheckSprite);

		Debug.Log("Loading finished! Press f7 to use the modpack creator ;)");

		HookUI();

	}


	#region UIHOOK

	public List<string> EnabledMods = new List<string>();
	public void HookUI()
	{
		GameObject MainMenuParent = GameObject.Find("MainMenuCanvas");

		GameObject MenuButtonsParent = MainMenuParent.transform.Find("MenuButtons").gameObject;

		GameObject NewGamePanelParent = MainMenuParent.transform.Find("New Game Box").gameObject;
		GameObject LoadGamePanelParent = MainMenuParent.transform.Find("Load Game Box").gameObject;

		GameObject CreateGameButton = MainMenuParent.transform.Find("New Game Box").transform.Find("CreateGameButton").gameObject;

		Debug.Log("Hooking UI");

		//Instantiating the selection panel





		//Debug.Log("Found parent");

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
		LoadWorldButton.GetComponent<Button>().onClick.AddListener(() => { Debug.Log("Opened load world Menu"); EnabledMods.Clear(); });



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

			foreach(LoadGame_Selection game_Selection in gameFiles)
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

			foreach(string mod in EnabledMods)
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

			foreach(string mod in EnabledMods)
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

	#region Main Menu
	public void LoadMainMenu(bool init)
	{
		IsMenuOpen = true;

		if (init)
		{

			Canvas = Instantiate(asset.LoadAsset<GameObject>("ModpackCreator_Canvas"), Vector3.zero, Quaternion.identity);
			DontDestroyOnLoad(Canvas);
			menu = Canvas.transform.Find("ModpackCreator_MainPanel").gameObject;

			//Buttons
			menu.transform.Find("ModpackCreator_Quitbutton").gameObject.GetComponent<Button>().onClick.AddListener(CloseMenu);
			menu.transform.Find("ModpackCreator_NewMod").gameObject.GetComponent<Button>().onClick.AddListener(NewModpack);
			menu.transform.Find("ModpackCreator_EditMod").gameObject.GetComponent<Button>().onClick.AddListener(EditModpack);
			menu.transform.Find("ModpackCreator_SaveMod").gameObject.GetComponent<Button>().onClick.AddListener(SaveModpack);
		}
		else
		{
			menu.SetActive(true);
		}

		var rmodFiles = Directory.EnumerateFiles(@"mods\");

		int i = 0;

		foreach (object rmod in rmodFiles)
		{

			if (Path.GetExtension(rmod.ToString()) == ".rmod")
			{

				InstalledRmods.Add(rmod.ToString());

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

							InstalledModNames.Add(ModInfo);

						}

					}
				}
				catch (Exception e)
				{
					Debug.LogWarning("Zip Error: " + e);
				}



				JObject ModinfoJson = JObject.Parse(ModInfo);

				GameObject ModlistContainer = menu.transform.Find("ModpackCreator_Modslist").gameObject.transform.Find("Modlist_Viewport").gameObject.transform.Find("Modlist_Container").gameObject;

				GameObject prefab = assetModlistModitem.LoadAsset<GameObject>("ModpackCreator_Modslist_Moditem");



				GameObject ModListEntryElem = Instantiate(prefab, ModlistContainer.transform);


				ModListEntryElem.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(ModListEntryElem.GetComponent<RectTransform>().anchoredPosition3D.x, ModListEntryElem.GetComponent<RectTransform>().anchoredPosition3D.y - i, ModListEntryElem.GetComponent<RectTransform>().anchoredPosition3D.z);


				//Debug.Log("Modinfojsonname: " + ModinfoJson["name"]);

				ModListEntryElem.transform.Find("Text").gameObject.GetComponent<Text>().text = ModinfoJson["name"].ToString();

				ModListEntryElem.GetComponent<Button>().onClick.AddListener(() => AddModToModpack(ModinfoJson["name"].ToString()));


				i += 30;











			}
		}

		CreateMessageBoxWin();




		//Write files for Datafiles (Modpack mod files)
		Datafiles.Add(@"ModPackCreatorData/" + "banner.jpg");
		Datafiles.Add(@"ModPackCreatorData/" + "data.txt");
		Datafiles.Add(@"ModPackCreatorData/" + "icon.png");
		Datafiles.Add(@"ModPackCreatorData/" + "modinfo.jsonfile");
		Datafiles.Add(@"ModPackCreatorData/" + "Modpacks.csfile");
		Datafiles.Add(@"ModPackCreatorData/" + "packages.config");
		Datafiles.Add(@"ModPackCreatorData/" + "modpackloader.assets");
	}


	public void CloseMenu()
	{
		IsMenuOpen = false;
		menu.SetActive(false);
	}


	#endregion
	#region MainmenuButtons
	public void NewModpack()
	{

		//Instantiate Modpack Panel and assign Functions
		NewModpackPanel = Instantiate(SetupModpackPanel.LoadAsset<GameObject>("ModpackCreator_SetupModpackPanel"), Canvas.transform);
		//Idk if useful but should prevent if raft has several main menu scenes

		DontDestroyOnLoad(NewModpackPanel);

		NewModpackPanel.transform.Find("SetupModpack_CreateModpack").GetComponent<Button>().onClick.AddListener(CreateNewModpack);
		NewModpackPanel.transform.Find("SetupModpack_ClosePanel").GetComponent<Button>().onClick.AddListener(CloseModpackPanel);




	}

	public void EditModpack()
	{

		EditModpackPanelGO = Instantiate(EditModpackPanel.LoadAsset<GameObject>("ModpackCreator_Editmodpackpanel"), Canvas.transform);

		EditModpackPanelGO.transform.Find("EditModpack_ClosePanel").GetComponent<Button>().onClick.AddListener(CloseEditModpackPanel);


		DontDestroyOnLoad(EditModpackPanelGO);

		InstalledRmods.Clear();
		InstalledModNames.Clear();

		var rmodFiles = Directory.EnumerateFiles(@"mods\");


		foreach (object rmod in rmodFiles)
		{

			if (Path.GetExtension(rmod.ToString()) == ".rmod")
			{

				InstalledRmods.Add(rmod.ToString());

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

							InstalledModNames.Add(ModinfoJson["name"].ToString());

						}

					}
				}
				catch (Exception e)
				{
					Debug.LogWarning("Zip Error: " + e);
				}
			}
		}



		//Load Mods list

		foreach (string modname in InstalledModNames)
		{

			GameObject ModlistContainer = EditModpackPanelGO.transform.Find("Editmodpackpanel_modlist").gameObject.transform.Find("Editmodpackpanel_modlist_Viewport").gameObject.transform.Find("Editmodpackpanel_modlist_Container").gameObject;

			GameObject prefab = assetModlistModitem.LoadAsset<GameObject>("ModpackCreator_Modslist_Moditem");


			GameObject ModListEntryElem = Instantiate(prefab, ModlistContainer.transform);


			ModListEntryElem.transform.Find("Text").gameObject.GetComponent<Text>().text = modname;

			ModListEntryElem.GetComponent<Button>().onClick.AddListener(() => LoadModpack(modname));

		}




	}




	#endregion


	#region NewModpackPanelFunctions
	public void CreateNewModpack()
	{
		string ModpackInputName = NewModpackPanel.transform.Find("SetupModpack_InputName").gameObject.transform.Find("Text").gameObject.GetComponent<Text>().text;
		//Banner and Icon will follow later


		if (!ModpackInputName.IsNullOrEmpty())
		{
			GameObject ModlistContainer = menu.transform.Find("ModpackCreator_ModpackContent").gameObject.transform.Find("ModpackContent_Viewport").gameObject.transform.Find("ModpackContent_Container").gameObject;

			foreach (Transform child in ModlistContainer.transform)
			{
				//I need other variable names ;D
				Destroy(child.gameObject);

			}
			//Set current vars
			CurrentModpackName = ModpackInputName;
			ModpackInputName = ModpackInputName.Replace(" ", "-");
			CurrentModpackPath = @"mods\" + ModpackInputName + ".rmod";

			//Debug.Log(CurrentModpackName);
			//Debug.Log(CurrentModpackPath);

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

	#region EditModpackPanelFunctions

	public void CloseEditModpackPanel()
	{
		Destroy(EditModpackPanelGO);
	}


	#endregion

	//SaveModpack Function
	public void SaveModpack()
	{
		if (CurrentModpackPath != null && CurrentModpackPath != null)
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

				CopyRmodsToModpack(temppath);

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

				CopyRmodsToModpack(temppath);

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

	public void LoadModpack(string Modpack)
	{
		GameObject ModlistContainer = menu.transform.Find("ModpackCreator_ModpackContent").gameObject.transform.Find("ModpackContent_Viewport").gameObject.transform.Find("ModpackContent_Container").gameObject;

		foreach (Transform child in ModlistContainer.transform)
		{
			//I need other variable names ;D
			Destroy(child.gameObject);

		}

		string tmpmodpack = Modpack.Replace(' ', '-');


		string rmod = Path.GetFullPath(@"mods\" + tmpmodpack + ".rmod");

		string datacontent = "";

		//Debug.Log(rmod);

		try
		{

			using (System.IO.Compression.ZipArchive archive = System.IO.Compression.ZipFile.Open(rmod, System.IO.Compression.ZipArchiveMode.Read))
			{
				System.IO.Compression.ZipArchiveEntry entry = archive.GetEntry("data.txt");

				using (StreamReader sr = new StreamReader(entry.Open()))
				{
					datacontent = sr.ReadToEnd();

				}

			}
		}
		catch (Exception e)
		{
			Debug.LogWarning("Zip Error: " + e);
			CreateMessageBox("Unexpected!", "The specified file is not a modpack");
			return;
		}



		List<string> mods = new List<string>();

		using (StringReader reader = new StringReader(datacontent))
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

		foreach (string moditem in modsarray)
		{
			AddModToModpack(moditem);
		}

		CurrentModpackName = Modpack;
		Modpack = Modpack.Replace(" ", "-");
		CurrentModpackPath = @"mods\" + Modpack + ".rmod";

		Destroy(EditModpackPanelGO);



	}


	public string GetModlistItems()
	{
		string Items = "";

		GameObject ModlistContainer = menu.transform.Find("ModpackCreator_ModpackContent").gameObject.transform.Find("ModpackContent_Viewport").gameObject.transform.Find("ModpackContent_Container").gameObject;





		foreach (Transform child in ModlistContainer.transform)
		{
			string textelem = child.transform.Find("ModpackCreator_Modeditor_ModitemElem").gameObject.transform.Find("Text").gameObject.GetComponent<Text>().text;
			Items += textelem + "\n";

		}


		return Items;
	}

	public string[] GetModlistItemsArray()
	{
		List<string> Items = new List<string>();

		GameObject ModlistContainer = menu.transform.Find("ModpackCreator_ModpackContent").gameObject.transform.Find("ModpackContent_Viewport").gameObject.transform.Find("ModpackContent_Container").gameObject;





		foreach (Transform child in ModlistContainer.transform)
		{
			string textelem = child.transform.Find("ModpackCreator_Modeditor_ModitemElem").gameObject.transform.Find("Text").gameObject.GetComponent<Text>().text;
			Items.Add(textelem);

		}


		return Items.ToArray();
	}

	public void CopyRmodsToModpack(string Temppath)
	{
		string destination = Temppath + @"Mods\";

		var ModlistItemsArr = GetModlistItemsArray();


		/*Debug.Log(InstalledRmods);
		Debug.Log(InstalledModNames);

		for(int i = 0; i < ModlistItemsArr.Length; i++)
		{
			var path = InstalledRmods[InstalledModNames.IndexOf(ModlistItemsArr[i])];

			File.Copy(path, destination + Path.GetFileName(path), true);
		}*/

		//The following is def. not the way to go. Real cpu intensive
		//Will patch that as soon as possible
		var rmodFiles = Directory.EnumerateFiles(@"mods\");
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
		}

		//Wrtie rmod data file
		string Items = "";



		foreach (var rmodFile in rmodFilesUsedArrr)
		{
			Items += Path.GetFileName(rmodFile) + "\n";
		}


		File.WriteAllText(Temppath + "rmoddata.txt", Items);


	}




	#region Add/Remove to/from Modpack
	public void AddModToModpack(string Modname)
	{
		//Check if it's in
		GameObject ModlistContainer = menu.transform.Find("ModpackCreator_ModpackContent").gameObject.transform.Find("ModpackContent_Viewport").gameObject.transform.Find("ModpackContent_Container").gameObject;

		foreach (Transform child in ModlistContainer.transform)
		{
			string textelem = child.transform.Find("ModpackCreator_Modeditor_ModitemElem").gameObject.transform.Find("Text").gameObject.GetComponent<Text>().text;
			if (textelem == Modname)
			{
				CreateMessageBox("Don't spam :)", "Can't have a mod twice in a modpack");
				return;

			}

		}


		GameObject ModeditorContainer = menu.transform.Find("ModpackCreator_ModpackContent").gameObject.transform.Find("ModpackContent_Viewport").gameObject.transform.Find("ModpackContent_Container").gameObject;
		GameObject prefab = assetModeditorModitem.LoadAsset<GameObject>("ModpackCreator_Modeditor_Moditem");

		GameObject ModeditorEntryElem = Instantiate(prefab, ModeditorContainer.transform);

		ModeditorEntryElem.transform.Find("ModpackCreator_Modeditor_ModitemElem").gameObject.transform.Find("Text").gameObject.GetComponent<Text>().text = Modname;

		//ModeditorEntryElem.transform.Find("Modeditor_DelElemButton").gameObject.GetComponent<Button>().onClick.AddListener(() => RemoveFromModpack(Modname));
		ModeditorEntryElem.transform.Find("ModpackCreator_Modeditor_ModitemElem").gameObject.GetComponent<Button>().onClick.AddListener(() => RemoveFromModpack(Modname));


	}

	public void RemoveFromModpack(string Modname)
	{
		GameObject ModlistContainer = menu.transform.Find("ModpackCreator_ModpackContent").gameObject.transform.Find("ModpackContent_Viewport").gameObject.transform.Find("ModpackContent_Container").gameObject;

		foreach (Transform child in ModlistContainer.transform)
		{
			string textelem = child.transform.Find("ModpackCreator_Modeditor_ModitemElem").gameObject.transform.Find("Text").gameObject.GetComponent<Text>().text;
			if (textelem == Modname)
			{
				//I need other variable names ;D
				Destroy(child.gameObject);

			}

		}
	}

	#endregion


	#region MessageBox
	public void CreateMessageBoxWin()
	{
		MessageBoxWindow = Instantiate(MessageBoxBundle.LoadAsset<GameObject>("ModpackCreator_MessageBox"), Canvas.transform);
		DontDestroyOnLoad(MessageBoxWindow);
		MessageBoxWindow.transform.Find("ModpackCreator_MessageBoxClose").GetComponent<Button>().onClick.AddListener(CloseMessageBox);
		MessageBoxWindow.SetActive(false);

	}

	public void CreateMessageBox(string Title, string Content)
	{
		MessageBoxWindow.SetActive(true);

		MessageBoxWindow.transform.Find("ModpackCreator_MessageBoxTitle").GetComponent<Text>().text = Title;
		MessageBoxWindow.transform.Find("ModpackCreator_MessageBoxContent").GetComponent<Text>().text = Content;
		MessageBoxWindow.GetComponent<RectTransform>().SetAsLastSibling();
	}
	public void CloseMessageBox()
	{
		MessageBoxWindow.SetActive(false);
	}
	#endregion


	public void OnModUnload()
	{
		asset.Unload(true);
		Debug.Log("Mod RaftModpackCreator has been unloaded!");
	}
}