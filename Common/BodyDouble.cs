using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;

using KKAPI;
using KKAPI.Chara;
using KKAPI.Maker;
using KKAPI.Utilities;

using ProloAPI;
using ProloAPI.Extensions;

using TMPro;

using UnityEngine;
using UnityEngine.SceneManagement;

using static ProloAPI.Utilities.PGeneral;
using static ProloAPI.Utilities.PGUI;

namespace BodyDouble
{
    #region dependencies
    [
    // Tell BepInEx that we need KKAPI to run, and that we need the latest version of it.
    // Check documentation of KoikatuAPI.VersionConst for more info.
    BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst),
    ]
    #endregion
    [BepInPlugin(GUID, ModName, Version)]
    public partial class BodyDouble_Core : ProloUnityPlugin<BodyDouble_Core>
    {

        public const string GUID = "prolo.bodydouble";//NEVER CHANGE THIS
        public const string ModName = "Body Double";
        public const string Version = "0.0.2";
        public const string Description =
            @"Adds the ability to save character cards to another " +
            @"character card and load by category (i.e. face, body, hair...).";

        public static new BodyDouble_Core Instance { get => ProloUnityPlugin<BodyDouble_Core>.Instance; }
        public static new ManualLogSource Logger { get => ProloUnityPlugin<BodyDouble_Core>.Logger; }

        #region Resources
        public static Texture2D UIGoku = null;
        public static Texture2D iconBG = null;
        #endregion

        public static BDConfig cfg;
        public struct BDConfig : IConfiguration
        {
            //Main
            public ConfigEntry<bool> enable { set; get; }
            public ConfigEntry<bool> areBodyDoublesPersistant { set; get; }
            public ConfigEntry<string> lastCardDir { get; set; }
            public ConfigEntry<KeyboardShortcut> openFloatingMenu { get; set; }

            // studio
            public ConfigEntry<bool> enableBGUI { get; set; }
            public ConfigEntry<bool> useCreatorDefaultBG { get; set; }
            public ConfigEntry<string> bgUIImagePath { get; set; }
            public ConfigEntry<float> floatingUIWidth { get; set; }
            public ConfigEntry<Rect> studioWinRec { get; set; }
            public ConfigEntry<Rect> studioSortOffset { get; set; }

            //maker
            public ConfigEntry<Rect> makerWinRec { get; set; }
            public ConfigEntry<Rect> makerSortOffset { get; set; }
            public ConfigEntry<float> viewportUISpace { get; set; } // For future use, if needed


            //Advanced
            public ConfigEntry<bool> resetOnLaunch { set; get; }
            public ConfigEntry<bool> debug { set; get; }
            public ConfigEntry<bool> enableTooltips { get; set; }
        }

        void ConfigInit()
        {
            #region Values
            int secIndex = 0;
            int secIndex2 = 99;
            int index = int.MaxValue;
            //bool enableBGUI = true;

            string main = "";
            //string mainx =
            //$"{secIndex++:d2}. " + main;

            string floating = "Floating GUI";
            string floatingx =
            $"{secIndex++:d2}. " + floating;

            string stud = "Studio";
            string studx =
            $"{secIndex++:d2}. " + stud;

            string adv = "Advanced";
            string advx =
            $"{secIndex2--:d2}. " + adv;
            #endregion

            ConfigFile binding = Instance.Config;
            cfg = new BDConfig()
            {
                //Main
                enable = binding.Bind(main, "Enable", true,
                new ConfigDescription("Enable or disable Body Double.", null,
                new ConfigurationManagerAttributes()
                {
                    Order = index--,
                    Category = main
                })),
                areBodyDoublesPersistant = binding.Bind(main, "Are BodyDoubles Persistant", false,
                new ConfigDescription("If enabled, Body Doubles will persist between sessions.", null,
                new ConfigurationManagerAttributes()
                {
                    Order = index--,
                    Category = main

                })),
                openFloatingMenu = binding.Bind(main, "Open Floating Menu Shortcut",
                new KeyboardShortcut(KeyCode.D, KeyCode.LeftControl, KeyCode.LeftShift),
                new ConfigDescription("The keyboard shortcut used to open the Body Double floating menu.", null,
                new ConfigurationManagerAttributes()
                {
                    Order = index--,
                    Category = main
                })),
                enableTooltips = binding.Bind<bool>(main, "Enable Tooltips", true,
                new ConfigDescription("If enabled, tooltips will be shown for Body Double Studio UI elements.", null,
                new ConfigurationManagerAttributes()
                {
                    Order = index--,
                    Category = main
                })),
                lastCardDir = binding.Bind(main, "Last Card Directory", "",
                new ConfigDescription("The last directory used to load or save Body Double Card.", null,
                new ConfigurationManagerAttributes()
                {
                    Order = index--,
                    Category = main,
                    IsAdvanced = true,
                })),

                //Floating GUI
                enableBGUI = binding.Bind<bool>(floating, "Enable Custom Background Image", false,
                new ConfigDescription("If enabled, the Body Double Studio UI will be available in the Character Maker Studio.", null,
                new ConfigurationManagerAttributes()
                {
                    Order = index--,
                    Category = floatingx
                })),
                useCreatorDefaultBG = binding.Bind<bool>(floating, "Use Creator Default Background", false,
                new ConfigDescription("If enabled, the default background of the Character Creator will be used instead of a custom image.", null,
                new ConfigurationManagerAttributes()
                {
                    Order = index--,
                    Category = floatingx
                })),
                bgUIImagePath = binding.Bind<string>(floating, "Background Image Path", "",
                new ConfigDescription("The path to the background image used in the Body Double Studio UI.", null,
                new ConfigurationManagerAttributes()
                {
                    Order = index--,
                    Category = floatingx
                })),
                floatingUIWidth = binding.Bind<float>(floating, "Floating UI Width", 0.5f,
                new ConfigDescription("The width of the Body Double Floating UI.", null,
                new ConfigurationManagerAttributes()
                {
                    Order = index--,
                    Category = floatingx,
                    IsAdvanced = true,
                })),
                makerWinRec = binding.Bind<Rect>(floating, "Maker Window Rect", new Rect(100, 100, 350, 500),
                new ConfigDescription("The position and size of the Body Double Maker UI window.", null,
                new ConfigurationManagerAttributes
                {
                    CustomDrawer = CustomRectDrawer(),
                    Order = index--,
                    Category = floatingx,
                    IsAdvanced = true,
                })),
                makerSortOffset = binding.Bind<Rect>(floating, "Maker Sort Offset", BodyDouble_GUI.offsetRect,
                new ConfigDescription("The offset applied to the sorting of the Body Double Maker UI.", null,
                new ConfigurationManagerAttributes
                {
                    CustomDrawer = CustomRectDrawer(),
                    Order = index--,
                    Category = floatingx,
                    IsAdvanced = true,
                })),

                //studio
                studioWinRec = binding.Bind<Rect>(studx, "Studio Window Rect", new Rect(100, 100, 350, 500),
                new ConfigDescription("The position and size of the Body Double Studio UI window.", null,
                new ConfigurationManagerAttributes
                {
                    CustomDrawer = CustomRectDrawer(),
                    Order = index--,
                    Category = studx,
                    IsAdvanced = true,
                })),
                studioSortOffset = binding.Bind<Rect>(stud, "Studio Sort Offset", BodyDouble_GUI.offsetRect,
                new ConfigDescription("The offset applied to the sorting of the Body Double Studio UI.", null,
                new ConfigurationManagerAttributes
                {
                    CustomDrawer = CustomRectDrawer(),
                    Order = index--,
                    Category = studx,
                    IsAdvanced = true,
                })),

                //Advanced 
                resetOnLaunch = binding.Bind<bool>(adv, "Reset On Launch", false,
                new ConfigDescription("If enabled, on the next launch all Body Double data will be reset. This value will then be reset to false.", null,
                new ConfigurationManagerAttributes()
                {
                    Order = index--,
                    Category = advx,
                    IsAdvanced = true,
                })),
                debug = binding.Bind<bool>(adv, "Debug", false,
                new ConfigDescription("If enabled, debug logs will be printed to the BepInEx log file.", null,
                new ConfigurationManagerAttributes()
                {
                    Order = index--,
                    Category = advx,
                    IsAdvanced = true,
                })),
                viewportUISpace = binding.Bind<float>(adv, "Viewport UI Space",
#if HONEY_API
                    0.39f,
#elif KOI_API
                    0.65f,
#endif
                new ConfigDescription("The percent of vertical space the scrollable Maker UI window will occupy in the viewport.", null,
                new ConfigurationManagerAttributes()
                {
                    Order = index--,
                    Category = advx,
                    IsAdvanced = true,
                })),

            };

            cfg.debug.DebugLink();

            BodyDouble_GUI.userTexUI = (cfg.bgUIImagePath.Value).CreateTexture();
            cfg.bgUIImagePath.SettingChanged += (m, n) =>
            {
                BodyDouble_GUI.userTexUI = (cfg.bgUIImagePath.Value).CreateTexture();
            };

            cfg.viewportUISpace.SettingChanged += (m, n) =>
            {
                BodyDouble_GUI.template.ResizeCustomUIViewport(cfg.viewportUISpace.Value);
            };

        }

        void ResourceInit()
        {

            //Embeded Resources
            using(MemoryStream memStream = new MemoryStream())
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resources = assembly.GetManifestResourceNames();

                ResourceGrabber("ultra instinct.jpg", assembly, resources, memStream);
                UIGoku = memStream?.GetBuffer()?.LoadTexture();

                ResourceGrabber("Super_Saiyan_Goku.png", assembly, resources, memStream);
                iconBG = memStream?.GetBuffer()?.LoadTexture();

                iconBG.Compress(false);
                iconBG.Apply();



            }

        }

        private void Awake()
        {
            ConfigInit();
            ResourceInit();

            CharacterApi.RegisterExtraBehaviour<BodyDouble_Controller>(GUID);
            BodyDouble_GUI.Init();
        }

        Scene lastScene;
        void Update()
        {
            if(cfg.openFloatingMenu.Value.IsDown())
                BodyDouble_GUI.enableImmediateUI = !BodyDouble_GUI.enableImmediateUI;

            if(SceneManager.GetActiveScene() != lastScene)
            {
                BodyDouble_GUI.enableImmediateUI = false;
                lastScene = SceneManager.GetActiveScene();
            }
        }
    }
}
// fdfadsfasff
