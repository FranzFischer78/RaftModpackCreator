﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace RaftModpackCreatorPages
{
	public class ModpackCreatePage:HMLLibrary.MenuPage
	{
		public static ModpackCreatePage instance;

		public static GameObject ModpackCreator_ModpackChoose;

		public static GameObject ModpackCreator_includemodtoggle;


		public void Start()
		{
			instance = this;
		}


		public static void UpdateModpackSelector(List<string> modpacklist, bool selectLastAdded)
		{
			Dropdown dropdownEl = ModpackCreator_ModpackChoose.GetComponent<Dropdown>();
			dropdownEl.ClearOptions();

			modpacklist.Insert(0, "CHOOSE A MODPACK");

			dropdownEl.AddOptions(modpacklist);
			//dropdownEl.value = dropdownEl.options.Count -1;

		}

	}
}
