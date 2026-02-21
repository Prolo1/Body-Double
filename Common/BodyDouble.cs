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
        public const string Version = "0.1.0";
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
            public ConfigEntry<string> lastCoordDir { get; set; }
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

            ConfigFile binding = Instance.Config;
            cfg = new BDConfig()
            {
                //Main
                enable = binding.Bind<bool>("", "Enable", true, "Enable or disable Body Double."),
                lastCoordDir = binding.Bind<string>("", "Last Coordinate Directory", "", "The last directory used to load or save Body Double coordinates."),
                areBodyDoublesPersistant = binding.Bind<bool>("", "Are BodyDoubles Persistant", false, "If enabled, Body Doubles will persist between sessions."),
                openFloatingMenu = binding.Bind<KeyboardShortcut>("", "Open Floating Menu Shortcut", new KeyboardShortcut(KeyCode.D, KeyCode.LeftControl, KeyCode.LeftShift), "The keyboard shortcut used to open the Body Double floating menu."),

                //studio
                enableBGUI = binding.Bind<bool>("", "Enable Body Double Studio UI", true, "If enabled, the Body Double Studio UI will be available in the Character Maker Studio."),
                useCreatorDefaultBG = binding.Bind<bool>("", "Use Creator Default Background", false, "If enabled, the default background of the Character Creator will be used instead of a custom image."),
                bgUIImagePath = binding.Bind<string>("", "Background Image Path", "", "The path to the background image used in the Body Double Studio UI."),
                floatingUIWidth = binding.Bind<float>("", "Studio UI Width", 0.5f, "The width of the Body Double Studio UI."),
                studioWinRec = binding.Bind<Rect>("", "Studio Window Rect", new Rect(100, 100, 350, 500), new ConfigDescription("The position and size of the Body Double Studio UI window.", null,
                new ConfigurationManagerAttributes { CustomDrawer = CustomRectDrawer() })),
                studioSortOffset = binding.Bind<Rect>("", "Studio Sort Offset", BodyDouble_GUI.offsetRect, new ConfigDescription("The offset applied to the sorting of the Body Double Studio UI.", null,
                new ConfigurationManagerAttributes { CustomDrawer = CustomRectDrawer() })),
                makerWinRec = binding.Bind<Rect>("", "Maker Window Rect", new Rect(100, 100, 350, 500), new ConfigDescription("The position and size of the Body Double Maker UI window.", null,
                new ConfigurationManagerAttributes { CustomDrawer = CustomRectDrawer() })),
                makerSortOffset = binding.Bind<Rect>("", "Maker Sort Offset", BodyDouble_GUI.offsetRect, new ConfigDescription("The offset applied to the sorting of the Body Double Maker UI.", null,
                new ConfigurationManagerAttributes { CustomDrawer = CustomRectDrawer() })),

                //Advanced 
                resetOnLaunch = binding.Bind<bool>("", "Reset On Launch", false, "If enabled, on the next launch all Body Double data will be reset. This value will then be reset to false."),
                debug = binding.Bind<bool>("", "Debug", false, "If enabled, debug logs will be printed to the BepInEx log file."),
                enableTooltips = binding.Bind<bool>("", "Enable Tooltips", true, "If enabled, tooltips will be shown for Body Double Studio UI elements."),
                viewportUISpace = binding.Bind<float>("", "Viewport UI Space", 0.65f, "The percent of vertical space the scrollable Maker UI window will occupy in the viewport."),

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

        void Update()
        {
            if(cfg.openFloatingMenu.Value.IsDown())
                BodyDouble_GUI.enableImmediateUI = !BodyDouble_GUI.enableImmediateUI;

        }
    }
}
// fdfadsfasff
