using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using ProloAPI;
using ProloAPI.Extensions;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using TMPro;

using BepInEx;

using HarmonyLib;

using KKAPI.Utilities;
using KKAPI.Maker;
using KKAPI.Maker.UI;
using KKAPI.Maker.UI.Sidebar;
using KKAPI.Studio.UI;
using KKAPI.Studio.UI.Toolbars;


using UniRx;

using Manager;

using Illusion.Game;
using Illusion.Extensions;

using System.Runtime.Remoting.Contexts;











#if HONEY_API
using AIChara;

using CharaCustom;
#else
using ChaCustom;
#endif

namespace BodyDouble
{
    using static BodyDouble_Controller;
    using static BodyDouble_Core;
    using static KKAPI.Maker.MakerAPI;
    using static KKAPI.Studio.StudioAPI;
    using static KKAPI.Utilities.GlobalTooltips;
    using static ProloAPI.Extensions.PGeneral;
    using static ProloAPI.Utilities.PGeneral;
    using static ProloAPI.Utilities.PGUI;
    using static SystemFileDialog;

    public class BodyDouble_GUI : ProloGUIBehaviour<BodyDouble_GUI>
    {
        #region Data
        static readonly string[] sortOptions = new[] { "default", "default rev.", "name", "name rev.", "Created", "Created rev.", "Updated", "Updated rev." };

        #region Main Game
        private static MakerCategory category = null;
        public static readonly string subCategoryName = "BodyDouble";
        public static readonly string displayName = "Body Double";

        static BodyDoubleData currentCoord = null;
        static PartFeilds partFields = new PartFeilds
        {
            body = true,
            face = true,
            hair = true,
            coordinate = true,
            perameter = false
        };
        static GridLayoutGroup gridLayout = null;
        static ToggleGroup tglGroup = null;
        static EventHandler isPersistantHndl = null;
        static EventHandler addAccessoriesHndl = null;
        public static MakerImage template = null;
        public static MyMakerText infoTxt = null;
        public static Button coordToFashionBtn = null;
        public static Button toFashionOnlyBtn = null;
#if HONEY_API
        public static CvsO_Type charaCustom { get; private set; } = null;
        public static CvsO_CharaSave charaSave { get; private set; } = null;
        public static CvsB_ShapeBreast boobCustom { get; private set; } = null;
        public static CvsB_ShapeWhole bodyCustom { get; private set; } = null;
        public static CvsF_ShapeWhole faceCustom { get; private set; } = null;
        public static CvsC_ClothesSave clothesSave { get; private set; } = null;
        public static CvsC_ClothesInput clothesInput { get; private set; } = null;
#else
        public static CvsChara charaCustom { get; private set; } = null;
        public static CustomFileWindow charaSave { get; private set; } = null;
        public static CvsBreast boobCustom { get; private set; } = null;
        public static CvsBodyShapeAll bodyCustom { get; private set; } = null;
        public static CvsFaceShapeAll faceCustom { get; private set; } = null;
        public static CvsClothes clothesSave { get; private set; } = null;
#endif
        #endregion

        #region Immediate GUI
        public static bool enableImmediateUI = false;
        public static bool enableImmediateUISort = false;
        public static Texture2D userTexUI = CreateColourTexture((Color.black * 0));
        public static UnityEvent customUI = new UnityEvent();
        public static Rect winRec = new Rect(105, 390, 440, 600);
        public static Rect offsetRect = new Rect(new Vector2(winRec.width, 0), new Vector2(200, 200));
        static Rect sortRect = new Rect(0, 0, 200, 150);
        static Func<int> sortDropdown = null;
        static int sortVal = -1;
        #endregion

        #region Studio
        static CurrentStateCategory categoryStudio;
        static SimpleToolbarToggle tgl;
        // static bool enableStudioUI = false;
        #endregion

        #endregion

        static void FloatingGUI()
        {
            //Studio

            if(!!StudioLoaded || !enableImmediateUI) return;


            //var camCtrl = Studio.Studio.Instance.cameraCtrl;

            var colour1 = GUI.color;
            var colour2 = GUI.contentColor;
            var colour3 = GUI.backgroundColor;

            GUI.color = Color.white;
            GUI.contentColor = Color.white;
            var bgTex = cfg.enableBGUI.Value ?
                    (cfg.useCreatorDefaultBG.Value ? UIGoku : userTexUI) :
                    (cfg.useCreatorDefaultBG.Value ? UIGoku : greyTex);

            GUI.DrawTexture(winRec = GUI.Window((GUID + "1").GetHashCode(),
                winRec, id =>
                {
                    //var studioCtrl = Studio.Studio.Instance;
                    //var camCtrl = studioCtrl.cameraCtrl;

                    customUI.Invoke();

                    winRec = IMGUIUtils.DragResizeEatWindow(id, winRec);


                    if(!cfg.makerWinRec.Value.Equals(winRec))
                        cfg.makerWinRec.Value = new Rect(winRec);
                }, ModName),
                bgTex,
                ScaleMode.StretchToFill);

            sortRect = new Rect(offsetRect.position + winRec.position, offsetRect.size);
            if(enableImmediateUISort)
                GUI.DrawTexture(sortRect = GUI.Window((GUID + "2").GetHashCode(), sortRect,
                    (id) =>
                    {
                        colour1 = GUI.color;
                        colour2 = GUI.contentColor;
                        colour3 = GUI.backgroundColor;

                        GUI.contentColor = Color.white;

                        if(sortDropdown == null)
                            sortDropdown = GUILayoutDropdownDrawerCreator
                            ((string[] x, int index) => new GUIContent() { text = x[index] }
                            , sortOptions, -1,
                            onSelect: (selected) =>
                            {
                                //enableStudioUISort = !enableStudioUISort; 
                                return selected;
                            });

                        sortVal = sortDropdown();

                        GUI.color = colour1;
                        GUI.contentColor = colour2;
                        GUI.backgroundColor = colour3;

                        GUI.DragWindow();
                        IMGUIUtils.EatInputInRect(sortRect);
                        offsetRect = new Rect(sortRect.position - winRec.position, sortRect.size);

                        if(offsetRect != cfg.makerSortOffset.Value)
                            cfg.makerSortOffset.Value = offsetRect;

                    }, "Sort Options"),
                    bgTex,
                    ScaleMode.StretchToFill);

            GUI.color = colour1;
            GUI.contentColor = colour2;
            GUI.backgroundColor = colour3;
        }

        static void StudioGUI()
        {
            //Studio

            if(!StudioLoaded || !enableImmediateUI) return;


            var camCtrl = Studio.Studio.Instance.cameraCtrl;

            var colour1 = GUI.color;
            var colour2 = GUI.contentColor;
            var colour3 = GUI.backgroundColor;

            GUI.color = Color.white;
            GUI.contentColor = Color.white;
            var bgTex = cfg.enableBGUI.Value ?
                    (cfg.useCreatorDefaultBG.Value ? UIGoku : userTexUI) :
                    (cfg.useCreatorDefaultBG.Value ? UIGoku : greyTex);

            GUI.DrawTexture(winRec = GUI.Window((GUID + "1").GetHashCode(),
                winRec, id =>
                {
                    //var studioCtrl = Studio.Studio.Instance;
                    //var camCtrl = studioCtrl.cameraCtrl;

                    customUI.Invoke();

                    winRec = IMGUIUtils.DragResizeEatWindow(id, winRec);


                    if(!cfg.studioWinRec.Value.Equals(winRec))
                        cfg.studioWinRec.Value = new Rect(winRec);
                }, ModName),
                bgTex,
                ScaleMode.StretchToFill);

            sortRect = new Rect(offsetRect.position + winRec.position, offsetRect.size);
            if(enableImmediateUISort)
                GUI.DrawTexture(sortRect = GUI.Window((GUID + "2").GetHashCode(), sortRect,
                    (id) =>
                    {
                        colour1 = GUI.color;
                        colour2 = GUI.contentColor;
                        colour3 = GUI.backgroundColor;

                        GUI.contentColor = Color.white;

                        if(sortDropdown == null)
                            sortDropdown = GUILayoutDropdownDrawerCreator
                            ((string[] x, int index) => new GUIContent() { text = x[index] }
                            , sortOptions, -1,
                            onSelect: (selected) =>
                            {
                                //enableStudioUISort = !enableStudioUISort; 
                                return selected;
                            });

                        sortVal = sortDropdown();

                        GUI.color = colour1;
                        GUI.contentColor = colour2;
                        GUI.backgroundColor = colour3;

                        GUI.DragWindow();
                        IMGUIUtils.EatInputInRect(sortRect);
                        offsetRect = new Rect(sortRect.position - winRec.position, sortRect.size);

                        if(offsetRect != cfg.studioSortOffset.Value)
                            cfg.studioSortOffset.Value = offsetRect;

                    }, "Sort Options"),
                    bgTex,
                    ScaleMode.StretchToFill);

            GUI.color = colour1;
            GUI.contentColor = colour2;
            GUI.backgroundColor = colour3;
        }

        public static void Init()
        {
            var inst = BodyDouble_Core.Instance;
            void OnLoad()
            {
                Logger.LogInfo($"Try make \"{nameof(BodyDouble_GUI)}\" object");
                if(Instance?.gameObject)
                    DestroyImmediate(Instance.gameObject);

                //Allow OnGUI() to run
                var obj = CreateGameObject(nameof(BodyDouble_GUI), typeof(BodyDouble_GUI));
                obj.transform.SetAsLastSibling();
                DontDestroyOnLoad(obj);
                Logger.LogInfo($"Finished making \"{nameof(BodyDouble_GUI)}\" object");
            }

            OnLoad();

            #region Init Values
            Dictionary<BodyDoubleData, Texture2D> bodies = new Dictionary<BodyDoubleData, Texture2D>();
            Vector2 scrollPos = Vector2.zero;
            BodyDoubleData selectKey = null;

            string tooltip = "";
            string search = "";
            int selectNum = -1;


            Vector2 toolPos = Vector2.zero;
            int selectedChar = 0;
            int skipFrames = -1;
            //BodyDouble_Controller bodCtrl = null;
            GUIStyle tmpSty = null;
            GUIStyle tabstyle = null;
            GUIStyle toggleStyle = null;
            Texture2D redTex = CreateColourTexture(Color.red);

            #endregion

            if(InsideStudio)
            {
                Instance.guiEvent.RemoveListener(StudioGUI);
                Instance.guiEvent.AddListener(StudioGUI);
                StudioLoadedChanged -= loadStudioChange;
                StudioLoadedChanged += loadStudioChange;
                void loadStudioChange(object sender, EventArgs e)
                {
                    ToolbarManager.AddLeftToolbarControl(tgl = new SimpleToolbarToggle("BodyDouble_GUI tgl", "", () => new Texture2D(32, 32), false, inst,
                        onValueChanged:
                        (val) => { enableImmediateUI = val; }).OnToolbarExists(gui =>
                        {
                            //Toggle image bi-pass 

                            iconBG.filterMode = FilterMode.Bilinear;
                            var btn = gui.ButtonObject;
                            btn.image.sprite =
                            Sprite.Create(iconBG,
                            new Rect(0, 0, iconBG.width, iconBG.height),
                            Vector2.one * .5f);

                            btn.image.color = Color.white;
                        })
                    );

                    #region Init Values
                    winRec = new Rect(cfg.studioWinRec.Value);
                    offsetRect = new Rect(cfg.studioSortOffset.Value);

                    partFields = new PartFeilds
                    {
                        body = true,
                        face = true,
                        hair = true,
                        coordinate = true,
                        perameter = false
                    };
                    #endregion

                    //GUI Update Loop
                    customUI.RemoveListener(act);
                    customUI.AddListener(act);
                    void act()
                    {

                        #region Init GUIStyles 
                        if(tabstyle == null)
                            tabstyle = new GUIStyle(GUI.skin.button)
                            {

                                padding = new RectOffset(5, 5, 5, 0),
                                alignment = TextAnchor.UpperLeft

                            };
                        if(toggleStyle == null)
                            toggleStyle = new GUIStyle(GUI.skin.toggle)
                            {

                                //padding = new RectOffset(5, 5, 5, 0),
                                // alignment = TextAnchor.UpperLeft

                            };

                        #endregion


                        GUILayout.BeginVertical();
                        try
                        {

                            #region Top

                            #region close btn
                            var colour1 = GUI.color.RGBMultipliedExt(1);
                            var colour2 = GUI.contentColor.RGBMultipliedExt(1);
                            var colour3 = GUI.backgroundColor.RGBMultipliedExt(1);

                            GUI.backgroundColor = (Color.red * 1).RGBMultipliedExt(.8f);

                            var btnSty = new GUIStyle(GUI.skin.button);
                            btnSty.normal.background = redTex;
                            btnSty.alignment = TextAnchor.MiddleCenter;

                            //btnSty.stretchHeight = true;
                            var btnSize = new Vector2(winRec.width * .1f, 15);
                            var btnPos = new Vector2(winRec.width - btnSize.x - 3, 3);
                            var myrect = new Rect(btnPos, btnSize);
                            if(GUI.Button(myrect, "X"))
                                enableImmediateUI = false;

                            GUI.color = colour1;
                            GUI.contentColor = colour2;
                            GUI.backgroundColor = colour3;
                            //	Logger.LogDebug($"close rect: {myrect}");
                            #endregion

                            #region Coordinate Lable
                            //Currently selected Coordinate
                            tmpSty = new GUIStyle(GUI.skin.label);

                            var sel = selectKey != null ?
                             selectKey.name : "";

                            tmpSty.fontStyle = selectKey == null ? FontStyle.Italic : FontStyle.Normal;
                            tmpSty.alignment = TextAnchor.LowerLeft;
                            tmpSty.wordWrap = true;
                            tmpSty.normal.textColor = tooltip.IsNullOrWhiteSpace() && selectKey != null ?
                            Color.green : Color.white;
                            sel = tooltip.IsNullOrWhiteSpace() ? sel : tooltip;

                            int fws = (int)(Mathf.Clamp(winRec.width, .001f, winRec.width) / 16);
                            tmpSty.fontSize = Math.Min(75, (int)(fws));

                            GUILayout.Label(sel, tmpSty, GUILayout.Height(tmpSty.lineHeight));
                            float txtH = GUILayoutUtility.GetLastRect().height;
                            #endregion

                            #region Search Bar
                            //Search Bar
                            GUILayout.BeginHorizontal();

                            tmpSty = new GUIStyle(GUI.skin.textField);
                            tmpSty.alignment = TextAnchor.LowerLeft;
                            tmpSty.fontSize = (int)(fws * 0.95f);
                            tmpSty.fontStyle = search.IsNullOrWhiteSpace() ? FontStyle.Italic : FontStyle.Normal;
                            //tmpSty.overflow= true;
                            tmpSty.wordWrap = true;

                            search = GUILayout.TextField(search, tmpSty,
                                GUILayout.Height(tmpSty.lineHeight));
                            if(search.IsNullOrEmpty())
                            {
                                tmpSty = new GUIStyle() { fontStyle = FontStyle.Italic, fontSize = fws };
                                tmpSty.normal.textColor = GUI.skin.textField.normal.textColor;
                                GUI.Label(GUILayoutUtility.GetLastRect(), "Search...", tmpSty);
                            }
                            txtH += GUILayoutUtility.GetLastRect().height;


                            //Sort Button
                            if(GUILayout.Button("Sort",
                                GUILayout.Width(winRec.width * .20f),
                                GUILayout.Height(tmpSty.lineHeight)))
                                enableImmediateUISort = !enableImmediateUISort;

                            GUILayout.EndHorizontal();
                            #endregion

                            #endregion

                            #region Mid
                            //Card view Window
                            scrollPos = GUILayout.BeginScrollView(scrollPos, false, true,
                                //GUILayout.Height((winRec.height - txtH) * .65f),
                                GUILayout.ExpandWidth(true),
                                GUILayout.ExpandHeight(true)
                                );

                            var lists = GetSelectedControllers<BodyDouble_Controller>();
                            var tmp =
                            lists.SelectMany(s => s.data.Values).Distinct()
                             .ToDictionary(k => k, v => bodies.TryGetValue(v, out var val1) ? val1 : null);

                            if(!tmp.Keys.SequenceEqual(bodies.Keys))
                            {
                                bodies = tmp;

                                //refresh all
                                foreach(var body in bodies.ToList())
                                    bodies[body.Key] = body.Key.cardData.LoadTexture();
                                if(cfg.debug.Value) Logger.LogDebug("the costumes have updated");
                            }

                            if(bodies.Count > 0)
                            {
                                var tmporder = bodies.
                                Where(val => (val.Key.translatedName + $" {val.Key.name}").Search(search.Replace(" ", ""))
                                || search.IsNullOrWhiteSpace()).ToList();

                                var sort =
                                new Func<KeyValuePair<BodyDoubleData, Texture2D>, object>
                                ((k) =>
                                {
                                    switch(sortVal / 2)
                                    {
                                    case 0:
                                        return (object)tmporder.IndexOf(k);
                                    case 1:
                                        return (object)k.Key.translatedName.ToLower().Trim();
                                    case 2:
                                        return (object)k.Key.created;
                                    case 3:
                                        return (object)k.Key.updated;
                                    default:
                                        return (object)tmporder.IndexOf(k);
                                    }
                                });

                                if(sortVal % 2 == 0)
                                    tmporder = tmporder.OrderBy(sort).ToList();
                                else
                                    tmporder = tmporder.OrderByDescending(sort).ToList();

                                GUIContent[] content = tmporder.Attempt(
                                   v => new GUIContent() { image = v.Value, tooltip = v.Key.translatedName ?? "???" }).ToArray();

                                var myStyle = new GUIStyle() { alignment = TextAnchor.LowerCenter };
                                myStyle.normal.textColor = (Color.white * 1).RGBMultipliedExt(.9f);
                                myStyle.focused.textColor = Color.cyan;
                                myStyle.wordWrap = true;

                                float w = (winRec.width - (100 * cfg.floatingUIWidth.Value));
                                float h = ((w == 0 ? .001f : w) / 3 * 1.5f * Mathf.Ceil(content.Length / 3.0f));
                                myStyle.fontSize = Mathf.CeilToInt(w / 18);
                                myStyle.padding.left = (int)(w * (1 / 3.0f) * .07f);
                                myStyle.padding.right = (int)(w * (1 / 3.0f) * .07f);
                                myStyle.padding.bottom = (int)(h / Mathf.Ceil(content.Length / 3.0f) * 0.12f);

                                selectNum = GUILayout.SelectionGrid(selectNum, content, 3,
                                    GUILayout.Width(w),
                                    GUILayout.Height(h));

                                //overlay
                                GUI.SelectionGrid(GUILayoutUtility.GetLastRect(), selectNum,
                                    content.Attempt(v => new GUIContent() { text = v.tooltip, tooltip = v.tooltip }).ToArray()
                                    , 3, myStyle);

                                selectKey = tmporder.InRange(selectNum) ?
                                tmporder.ElementAt(selectNum).Key : null;

                                // tooltip = GUI.mouseTooltip;
                            }
                            else
                            {
                                selectNum = -1;
                                selectKey = null;
                                tooltip = "";
                            }
                            GUILayout.EndScrollView();
                            #endregion

                            #region Bot
                            //Bottom Buttons
                            GUILayout.BeginVertical(GUILayout.Height(winRec.height * .15f));

                            colour1 = GUI.color;
                            colour2 = GUI.contentColor;
                            colour3 = GUI.backgroundColor;

                            GUI.color = Color.white;
                            GUI.contentColor = Color.white;


                            //Section Load Toggles
                            GUILayout.BeginScrollView(new Vector2());
                            GUILayout.BeginHorizontal();
                            partFields.body = GUILayout.Toggle(partFields.body, "Body", toggleStyle);
                            partFields.face = GUILayout.Toggle(partFields.face, "Face", toggleStyle);
                            partFields.hair = GUILayout.Toggle(partFields.hair, "Hair", toggleStyle);
                            partFields.coordinate = GUILayout.Toggle(partFields.coordinate, "Coordinate", toggleStyle);
                            partFields.perameter = GUILayout.Toggle(partFields.perameter, "Perameter", toggleStyle);
                            GUILayout.EndHorizontal();
                            GUILayout.EndScrollView();

                            tmpSty = new GUIStyle(GUI.skin.button);
                            tmpSty.normal.textColor = tmpSty.normal.textColor.AlphaMultipliedExt(lists.Any() ? 1 : 0.60f);
                            if(!lists.Any())
                                tmpSty.active = tmpSty.hover = tmpSty.normal;

                            var persist =
                             GUILayout.Toggle(cfg.areBodyDoublesPersistant.Value,
                             GUIContent.Temp("Is Persistant", cfg.areBodyDoublesPersistant.Description.Description), toggleStyle);

                            if(persist != cfg.areBodyDoublesPersistant.Value)
                                cfg.areBodyDoublesPersistant.Value = persist;

                            GUILayout.BeginHorizontal();
                            if(GUILayout.Button("Use Selected BodyDouble", tmpSty))
                                foreach(var fashion in lists)
                                    if(selectKey != null)
                                        fashion.LoadBodyDouble(selectKey, partFields);

                            if(GUILayout.Button("Use Default BodyDouble", tmpSty))
                                foreach(var fashion in lists)
                                    fashion.LoadDefaultBodyDouble(partFields);
                            GUILayout.EndHorizontal();

                            GUILayout.BeginHorizontal();

                            if(GUILayout.Button("Add Card[s]", tmpSty) && lists.Any())
                            {
                                ForeGrounder.SetCurrentForground();
                                GetNewPresetImages(null);
                                ForeGrounder.RevertForground();
                            }
                            //if(GUILayout.Button("load current coordinate"));

                            GUILayout.EndHorizontal();

                            GUI.color = Color.white;
                            GUI.contentColor = Color.red;
                            GUI.backgroundColor = (Color.white * 1).RGBMultipliedExt(0.35f);

                            GUILayout.Space(5);
                            GUILayout.Label("DANGER ZONE");
                            GUILayout.Space(5);

                            GUILayout.BeginHorizontal();
                            if(GUILayout.Button("Remove selected"))
                                foreach(var fashion in lists)
                                    if(selectKey != null)
                                        fashion.RemoveBodyDouble(selectKey);

                            if(GUILayout.Button("Remove All"))
                                foreach(var bodDub in lists)
                                    foreach(var all in bodDub.data.Values.ToList())
                                        bodDub.RemoveBodyDouble(all);

                            GUI.color = colour1;
                            GUI.contentColor = colour2;
                            GUI.backgroundColor = colour3;

                            GUILayout.EndHorizontal();

                            GUILayout.EndVertical();//bot vertical
                            #endregion
                        }
                        catch(Exception ex) { Logger.LogError(ex); }

                        GUILayout.EndVertical();

                        if(cfg.enableTooltips.Value)
                            IMGUIUtils.DrawTooltip(winRec, (int)(winRec.width * .75f));

                    }
                }
            }
            else
            {
                #region Maker
                RegisterCustomSubCategories -= MakerAPI_RegisterCustomSubCategories;
                RegisterCustomSubCategories += MakerAPI_RegisterCustomSubCategories;
                MakerBaseLoaded += (s, e) =>
                {
                    AddBodyDoubleMenu_Maker(e);
                    ////Maker update loop (would be deleted otherwise)
                    //customUI.AddListener(act);
                };
                MakerFinishedLoading += (s, e) =>
                {
                    var allCvs =

#if HONEY_API
                    ((CvsSelectWindow[])UnityEngine.Resources.FindObjectsOfTypeAll<CvsSelectWindow>())
                    .OrderBy((k) => k.transform.GetSiblingIndex())//I just want them in the right order
                    .Attempt(p => p.items)
                    .Aggregate((l, r) => l.Concat(r).ToArray());//should flaten array


                    bodyCustom = (CvsB_ShapeWhole)allCvs.FirstOrNull((p) => p.cvsBase is CvsB_ShapeWhole)?.cvsBase;
                    faceCustom = (CvsF_ShapeWhole)allCvs.FirstOrNull((p) => p.cvsBase is CvsF_ShapeWhole)?.cvsBase;
                    boobCustom = (CvsB_ShapeBreast)allCvs.FirstOrNull((p) => p.cvsBase is CvsB_ShapeBreast)?.cvsBase;
                    charaCustom = (CvsO_Type)allCvs.FirstOrNull((p) => p.cvsBase is CvsO_Type)?.cvsBase;
                    charaSave = (CvsO_CharaSave)allCvs.FirstOrNull((p) => p.cvsBase is CvsO_CharaSave)?.cvsBase;

                    clothesSave = (CvsC_ClothesSave)(allCvs.FirstOrNull((p) => p.cvsBase is CvsC_ClothesSave)?.cvsBase);
                    //	clothesInput = (CvsC_ClothesInput)(allCvs.FirstOrNull((p) => p.cvsBase is CvsC_ClothesInput)?.cvsBase);


#else
                0;//don't remove this!
                    bodyCustom = (CvsBodyShapeAll)Resources.FindObjectsOfTypeAll(typeof(CvsBodyShapeAll))[allCvs];
                    faceCustom = (CvsFaceShapeAll)Resources.FindObjectsOfTypeAll(typeof(CvsFaceShapeAll))[allCvs];
                    boobCustom = (CvsBreast)Resources.FindObjectsOfTypeAll(typeof(CvsBreast))[allCvs];
                    charaSave = (CustomFileWindow)Resources.FindObjectsOfTypeAll(typeof(CustomFileWindow))[allCvs];
                    charaCustom = (CvsChara)Resources.FindObjectsOfTypeAll(typeof(CvsChara))[allCvs];

#endif

#if HONEY_API
                    //add new buttons to coordinate save screen
                    {
                        var orig = charaSave.charaLoadWin.button[1];
                        var par = orig.transform.parent;

                        coordToFashionBtn = GameObject
                        .Instantiate(orig.gameObject, par)
                        .GetComponent<Button>();



                        var parRec = coordToFashionBtn.GetComponent<RectTransform>();
                        parRec.pivot = Vector3.one * .5f;
                        parRec.anchoredPosition =
                        parRec.anchoredPosition +
                        new Vector2(parRec.rect.size.x * .5f,
                        parRec.rect.size.y * -.5f - 80);



                        Text txt;
                        GameObject obj;
                        if(txt = coordToFashionBtn.GetComponentInChildren<Text>())
                        {
                            obj = txt.gameObject;
                            GameObject.DestroyImmediate(txt);
                        }
                        else
                            obj = coordToFashionBtn.GetComponentInChildren<TMP_Text>()?.gameObject;


                        var txtpro = obj.GetOrAddComponent<TextMeshProUGUI>();
                        txtpro.ScaleToParent2D(pwidth: .8f);
                        txtpro.autoSizeTextContainer = false;
                        txtpro.extraPadding = true;
                        txtpro.alignment = TextAlignmentOptions.Center;
                        txtpro.fontStyle = FontStyles.Bold;
                        txtpro.color = Color.black;
                        txtpro.enableAutoSizing = true;
                        txtpro.fontSizeMax = 100;
                        txtpro.fontSizeMin = 1;
                        txtpro.SetAllDirty();

                        coordToFashionBtn.SetTextFromTextComponent("Save & Add to BodyDouble");
                        coordToFashionBtn.onClick.ActuallyRemoveAllListeners();

                        toFashionOnlyBtn = GameObject
                        .Instantiate(coordToFashionBtn.gameObject, par)
                        .GetComponent<Button>();
                        toFashionOnlyBtn.GetComponent<RectTransform>().anchoredPosition =
                        toFashionOnlyBtn.GetComponent<RectTransform>().anchoredPosition +
                        new Vector2(toFashionOnlyBtn.GetComponent<RectTransform>().rect.size.x + 5, 0);
                        toFashionOnlyBtn.SetTextFromTextComponent("Add Only to BodyDouble ");

                        //Add buttons to original list 
                        clothesSave.clothesLoadWin.button.Append(coordToFashionBtn);
                        clothesSave.clothesLoadWin.button.Append(toFashionOnlyBtn);

                        try
                        {

                            orig.ObserveEveryValueChanged((j) => j.m_Interactable).Subscribe((inter) =>
                            {
                                if(!coordToFashionBtn) return;
                                if(!toFashionOnlyBtn) return;
                                toFashionOnlyBtn.interactable = coordToFashionBtn.interactable = inter;
                            });
                        }
                        catch(Exception ex)
                        {
                            Logger.LogError("could not subscribe to value change: " + ex);
                        }

                        //Onclick code is done in a hook...

                        //var HLGroup = par.gameObject.GetComponent<HorizontalLayoutGroup>();
                        ////HLGroup.CalculateLayoutInputHorizontal();
                        ////HLGroup.CalculateLayoutInputVertical();
                        //HLGroup.SetDirty();
                    }

                    //Force the floating settings window to show up
                    {
                        var btn = allCvs?.FirstOrNull(p => p?.btnItem?.gameObject?.GetTextFromTextComponent() == displayName).btnItem;
                        btn?.onClick?.AddListener(() => GetMakerBase().drawMenu.ChangeMenuFunc());
                    }
#elif KOI_API
                    //add new buttons to coordinate save screen
                    {
                        var orig = charaSave.btnSave;
                        var par = orig.transform.parent;

                        coordToFashionBtn = GameObject
                        .Instantiate(orig.gameObject, par)
                        .GetComponent<Button>();
                        coordToFashionBtn.name = "Btn_SaveToBodyDouble";


                        var parRec = coordToFashionBtn.GetComponent<RectTransform>();
                        parRec.pivot = Vector3.one * .5f;
                        parRec.anchoredPosition =
                        parRec.anchoredPosition +
                        new Vector2(parRec.rect.size.x * .5f,
                        parRec.rect.size.y * -.5f - 80);


                        Text txt;
                        GameObject obj;
                        if(txt = coordToFashionBtn.GetComponentInChildren<Text>())
                        {
                            obj = txt.gameObject;
                            GameObject.DestroyImmediate(txt);
                        }
                        else
                            obj = coordToFashionBtn.GetComponentInChildren<TMP_Text>()?.gameObject;


                        var txtpro = obj.GetOrAddComponent<TextMeshProUGUI>();
                        txtpro.ScaleToParent2D(pwidth: .8f);
                        txtpro.autoSizeTextContainer = false;
                        txtpro.extraPadding = true;
                        txtpro.alignment = TextAlignmentOptions.Center;
                        txtpro.fontStyle = FontStyles.Bold;
                        txtpro.color = Color.black;
                        txtpro.enableAutoSizing = true;
                        txtpro.fontSizeMax = 100;
                        txtpro.fontSizeMin = 1;
                        txtpro.SetAllDirty();

                        coordToFashionBtn.SetTextFromTextComponent("Save & Add to BodyDouble");
                        coordToFashionBtn.onClick.ActuallyRemoveAllListeners();

                        toFashionOnlyBtn = GameObject
                        .Instantiate(coordToFashionBtn.gameObject, par)
                        .GetComponent<Button>();
                        toFashionOnlyBtn.name = "Btn_AddOnlyToBodyDouble";
                        toFashionOnlyBtn.GetComponent<RectTransform>().anchoredPosition =
                        toFashionOnlyBtn.GetComponent<RectTransform>().anchoredPosition +
                        new Vector2(toFashionOnlyBtn.GetComponent<RectTransform>().rect.size.x + 5, 0);
                        toFashionOnlyBtn.SetTextFromTextComponent("Add Only to BodyDouble ");

                        ////Add buttons to original list 
                        //clothesSave.clothesLoadWin.button.Append(coordToFashionBtn);
                        //clothesSave.clothesLoadWin.button.Append(toFashionOnlyBtn);

                        try
                        {

                            orig.ObserveEveryValueChanged((j) => j.interactable).Subscribe((inter) =>
                            {
                                if(!coordToFashionBtn) return;
                                if(!toFashionOnlyBtn) return;
                                toFashionOnlyBtn.interactable = coordToFashionBtn.interactable = inter;
                            });
                        }
                        catch(Exception ex)
                        {
                            Logger.LogError("could not subscribe to value change: " + ex);
                        }

                        //Onclick code is done in a hook...

                        //var HLGroup = par.gameObject.GetComponent<HorizontalLayoutGroup>();
                        ////HLGroup.CalculateLayoutInputHorizontal();
                        ////HLGroup.CalculateLayoutInputVertical();
                        //HLGroup.SetDirty();
                    }

                    ////Force the floating settings window to show up
                    //{
                    //    var btn = allCvs?.FirstOrNull(p => p?.btnItem?.gameObject?.GetTextFromTextComponent() == displayName).btnItem;
                    //    btn?.onClick?.AddListener(() => GetMakerBase().drawMenu.ChangeMenuFunc());
                    //}
#endif

                };
                MakerExiting += (s, e) =>
                {
                    //Cleanup();
                };
                void MakerAPI_RegisterCustomSubCategories(object sender, RegisterSubCategoriesEvent e)
                {

#if HONEY_API
                    category = KKAPI.Maker.MakerConstants.Parameter.Type;
#else
                    category = KKAPI.Maker.MakerConstants.Parameter.Character;
#endif
                    e.AddSubCategory(category = new MakerCategory(category.CategoryName, subCategoryName, displayName: displayName));


                    MakerAPI.AddSidebarControl(new SidebarToggle("Show Floating BodyDouble", enableImmediateUI, inst))
                        .OnGUIExists(gui =>
                        {
                            gui.ValueChanged.Subscribe(
                                (val) =>
                                {
                                    enableImmediateUI = val;
                                }
                            );
                        });
                }
                #endregion

                Instance.guiEvent.RemoveListener(FloatingGUI);
                Instance.guiEvent.AddListener(FloatingGUI);


                #region Init Values
                winRec = new Rect(cfg.makerWinRec.Value);
                offsetRect = new Rect(cfg.makerSortOffset.Value);
                partFields = new PartFeilds
                {
                    body = true,
                    face = true,
                    hair = true,
                    coordinate = true,
                    perameter = false
                };
                #endregion

                //GUI Update Loop
                customUI.RemoveListener(act);
                customUI.AddListener(act);
                void act()
                {

                    #region Init GUIStyles 
                    if(tabstyle == null)
                        tabstyle = new GUIStyle(GUI.skin.button)
                        {

                            padding = new RectOffset(5, 5, 5, 0),
                            alignment = TextAnchor.UpperLeft

                        };
                    if(toggleStyle == null)
                        toggleStyle = new GUIStyle(GUI.skin.toggle)
                        {

                            //padding = new RectOffset(5, 5, 5, 0),
                            // alignment = TextAnchor.UpperLeft

                        };

                    #endregion


                    GUILayout.BeginVertical();
                    try
                    {

                        #region Top

                        #region close btn
                        var colour1 = GUI.color.RGBMultipliedExt(1);
                        var colour2 = GUI.contentColor.RGBMultipliedExt(1);
                        var colour3 = GUI.backgroundColor.RGBMultipliedExt(1);

                        GUI.backgroundColor = (Color.red * 1).RGBMultipliedExt(.8f);

                        var btnSty = new GUIStyle(GUI.skin.button);
                        btnSty.normal.background = redTex;
                        btnSty.alignment = TextAnchor.MiddleCenter;

                        //btnSty.stretchHeight = true;
                        var btnSize = new Vector2(winRec.width * .1f, 15);
                        var btnPos = new Vector2(winRec.width - btnSize.x - 3, 3);
                        var myrect = new Rect(btnPos, btnSize);
                        if(GUI.Button(myrect, "X"))
                            enableImmediateUI = false;

                        GUI.color = colour1;
                        GUI.contentColor = colour2;
                        GUI.backgroundColor = colour3;
                        //	Logger.LogDebug($"close rect: {myrect}");
                        #endregion

                        #region Coordinate Lable
                        //Currently selected Coordinate
                        tmpSty = new GUIStyle(GUI.skin.label);

                        var sel = selectKey != null ?
                         selectKey.name : "";

                        tmpSty.fontStyle = selectKey == null ? FontStyle.Italic : FontStyle.Normal;
                        tmpSty.alignment = TextAnchor.LowerLeft;
                        tmpSty.wordWrap = true;
                        tmpSty.normal.textColor = tooltip.IsNullOrWhiteSpace() && selectKey != null ?
                        Color.green : Color.white;
                        sel = tooltip.IsNullOrWhiteSpace() ? sel : tooltip;

                        int fws = (int)(Mathf.Clamp(winRec.width, .001f, winRec.width) / 16);
                        tmpSty.fontSize = Math.Min(75, (int)(fws));

                        GUILayout.Label(sel, tmpSty, GUILayout.Height(tmpSty.lineHeight));
                        float txtH = GUILayoutUtility.GetLastRect().height;
                        #endregion

                        #region Search Bar & Sort Button
                        //Search Bar 
                        GUILayout.BeginHorizontal();

                        tmpSty = new GUIStyle(GUI.skin.textField);
                        tmpSty.alignment = TextAnchor.LowerLeft;
                        tmpSty.fontSize = (int)(fws * 0.95f);
                        tmpSty.fontStyle = search.IsNullOrWhiteSpace() ? FontStyle.Italic : FontStyle.Normal;
                        //tmpSty.overflow= true;
                        tmpSty.wordWrap = true;

                        search = GUILayout.TextField(search, tmpSty,
                            GUILayout.Height(tmpSty.lineHeight));
                        if(search.IsNullOrEmpty())
                        {
                            tmpSty = new GUIStyle() { fontStyle = FontStyle.Italic, fontSize = fws };
                            tmpSty.normal.textColor = GUI.skin.textField.normal.textColor;
                            GUI.Label(GUILayoutUtility.GetLastRect(), "Search...", tmpSty);
                        }
                        txtH += GUILayoutUtility.GetLastRect().height;


                        //Sort Button
                        if(GUILayout.Button("Sort",
                            GUILayout.Width(winRec.width * .20f),
                            GUILayout.Height(tmpSty.lineHeight)))
                            enableImmediateUISort = !enableImmediateUISort;

                        GUILayout.EndHorizontal();
                        #endregion

                        #endregion

                        #region Mid
                        //Card view Window
                        scrollPos = GUILayout.BeginScrollView(scrollPos, false, true,
                            //GUILayout.Height((winRec.height - txtH) * .65f),
                            GUILayout.ExpandWidth(true),
                            GUILayout.ExpandHeight(true)
                            );

                        //Character choices
                        var lists = (IEnumerable<BodyDouble_Controller>)null;

                        try
                        {
#if KKS
                            lists = Character.GetCharaList(1).Concat(Character.GetCharaList(0)).Select(ctrl => ctrl.GetComponent<BodyDouble_Controller>());
#else
                            lists = Character.Instance.GetCharaList(1).Concat(Character.Instance.GetCharaList(0)).Select(ctrl => ctrl.GetComponent<BodyDouble_Controller>());
#endif
                        }
                        catch(Exception e) { Logger.LogError($"List did not initialize\n{e}"); }

                        var tmp =
                        lists.SelectMany(s => s.data.Values).Distinct()
                         .ToDictionary(k => k, v => bodies.TryGetValue(v, out var val1) ? val1 : null);

                        if(!tmp.Keys.SequenceEqual(bodies.Keys))
                        {
                            bodies = tmp;

                            //refresh all
                            foreach(var body in bodies.ToList())
                                bodies[body.Key] = body.Key.cardData.LoadTexture();
                            if(cfg.debug.Value) Logger.LogDebug("the costumes have updated");
                        }

                        if(bodies.Count > 0)
                        {
                            var tmporder = bodies.
                            Where(val => (val.Key.translatedName + $" {val.Key.name}").Search(search.Replace(" ", ""))
                            || search.IsNullOrWhiteSpace()).ToList();

                            var sort =
                            new Func<KeyValuePair<BodyDoubleData, Texture2D>, object>
                            ((k) =>
                            {
                                switch(sortVal / 2)
                                {
                                case 0:
                                    return (object)tmporder.IndexOf(k);
                                case 1:
                                    return (object)k.Key.translatedName.ToLower().Trim();
                                case 2:
                                    return (object)k.Key.created;
                                case 3:
                                    return (object)k.Key.updated;
                                default:
                                    return (object)tmporder.IndexOf(k);
                                }
                            });

                            if(sortVal % 2 == 0)
                                tmporder = tmporder.OrderBy(sort).ToList();
                            else
                                tmporder = tmporder.OrderByDescending(sort).ToList();

                            GUIContent[] content = tmporder.Attempt(
                               v => new GUIContent() { image = v.Value, tooltip = v.Key.translatedName ?? "???" }).ToArray();

                            var myStyle = new GUIStyle() { alignment = TextAnchor.LowerCenter };
                            myStyle.normal.textColor = (Color.white * 1).RGBMultipliedExt(.9f);
                            myStyle.focused.textColor = Color.cyan;
                            myStyle.wordWrap = true;

                            float w = (winRec.width - (100 * cfg.floatingUIWidth.Value));
                            float h = ((w == 0 ? .001f : w) / 3 * 1.5f * Mathf.Ceil(content.Length / 3.0f));
                            myStyle.fontSize = Mathf.CeilToInt(w / 18);
                            myStyle.padding.left = (int)(w * (1 / 3.0f) * .07f);
                            myStyle.padding.right = (int)(w * (1 / 3.0f) * .07f);
                            myStyle.padding.bottom = (int)(h / Mathf.Ceil(content.Length / 3.0f) * 0.12f);

                            //main grid
                            selectNum = GUILayout.SelectionGrid(selectNum, content, 3,
                                GUILayout.Width(w),
                                GUILayout.Height(h));

                            //overlay
                            GUI.SelectionGrid(GUILayoutUtility.GetLastRect(), selectNum,
                                content.Attempt(v => new GUIContent() { text = v.tooltip, tooltip = v.tooltip }).ToArray()
                                , 3, myStyle);

                            selectKey = tmporder.InRange(selectNum) ?
                            tmporder.ElementAt(selectNum).Key : null;

                            // tooltip = GUI.mouseTooltip;
                        }
                        else
                        {
                            selectNum = -1;
                            selectKey = null;
                            tooltip = "";
                        }
                        GUILayout.EndScrollView();
                        #endregion

                        #region Bot
                        //Bottom Buttons
                        GUILayout.BeginVertical(GUILayout.Height(winRec.height * .15f));

                        colour1 = GUI.color;
                        colour2 = GUI.contentColor;
                        colour3 = GUI.backgroundColor;

                        GUI.color = Color.white;
                        GUI.contentColor = Color.white;


                        //Section Load Toggles
                        GUILayout.BeginScrollView(new Vector2());
                        GUILayout.BeginHorizontal();
                        partFields.body = GUILayout.Toggle(partFields.body, "Body", toggleStyle);
                        partFields.face = GUILayout.Toggle(partFields.face, "Face", toggleStyle);
                        partFields.hair = GUILayout.Toggle(partFields.hair, "Hair", toggleStyle);
                        partFields.coordinate = GUILayout.Toggle(partFields.coordinate, "Coordinate", toggleStyle);
                        partFields.perameter = GUILayout.Toggle(partFields.perameter, "Perameter", toggleStyle);
                        GUILayout.EndHorizontal();
                        GUILayout.EndScrollView();

                        tmpSty = new GUIStyle(GUI.skin.button);
                        tmpSty.normal.textColor = tmpSty.normal.textColor.AlphaMultipliedExt(lists.Any() ? 1 : 0.60f);
                        if(!lists.Any())
                            tmpSty.active = tmpSty.hover = tmpSty.normal;

                        var persist =
                            GUILayout.Toggle(cfg.areBodyDoublesPersistant.Value,
                            GUIContent.Temp("Is Persistant", cfg.areBodyDoublesPersistant.Description.Description), toggleStyle);
                        if(persist != cfg.areBodyDoublesPersistant.Value)
                            cfg.areBodyDoublesPersistant.Value = persist;




                        GUILayout.BeginHorizontal();
                        if(GUILayout.Button("Use Selected BodyDouble", tmpSty))
                            foreach(var fashion in lists)
                                if(selectKey != null)
                                    fashion.LoadBodyDouble(selectKey, partFields);

                        if(GUILayout.Button("Use Default BodyDouble", tmpSty))
                            foreach(var fashion in lists)
                                fashion.LoadDefaultBodyDouble(partFields);
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();

                        if(GUILayout.Button("Add Card[s]", tmpSty) && lists.Any())
                        {
                            ForeGrounder.SetCurrentForground();
                            GetNewPresetImages(null);
                            ForeGrounder.RevertForground();
                        }
                        //if(GUILayout.Button("load current coordinate"));

                        GUILayout.EndHorizontal();

                        GUI.color = Color.white;
                        GUI.contentColor = Color.red;
                        GUI.backgroundColor = (Color.white * 1).RGBMultipliedExt(0.35f);

                        GUILayout.Space(5);
                        GUILayout.Label("DANGER ZONE");
                        GUILayout.Space(5);

                        GUILayout.BeginHorizontal();
                        if(GUILayout.Button("Remove selected"))
                            foreach(var fashion in lists)
                                if(selectKey != null)
                                    fashion.RemoveBodyDouble(selectKey);

                        if(GUILayout.Button("Remove All"))
                            foreach(var bodDub in lists)
                                foreach(var all in bodDub.data.Values.ToList())
                                    bodDub.RemoveBodyDouble(all);

                        GUI.color = colour1;
                        GUI.contentColor = colour2;
                        GUI.backgroundColor = colour3;

                        GUILayout.EndHorizontal();

                        GUILayout.EndVertical();//bot vertical
                        #endregion
                    }
                    catch(Exception ex) { Logger.LogError(ex); }

                    GUILayout.EndVertical();

                    if(cfg.enableTooltips.Value)
                        IMGUIUtils.DrawTooltip(winRec, (int)(winRec.width * .75f));

                }
            }

        }

        private static void AddBodyDoubleMenu_Maker(RegisterCustomControlsEvent e)
        {

            var inst = BodyDouble_Core.Instance;
            var bdCtrl = GetCharacterControl().GetComponent<BodyDouble_Controller>();

            #region Init
#if KOI_API
            e.AddControl<MakerText>(new MakerText(displayName, category, inst));
            e.AddControl(new MakerSeparator(category, inst));
#endif
            //Create template for costume cards
            template = e.AddControl(new MakerImage(Texture2D.blackTexture, category, inst));
            template.OnGUIExists((gui) =>
            {
                IEnumerator SetupCo()
                {
                    yield return new WaitWhile(() => gui?.ControlObject?.GetComponentInParent<ScrollRect>()?.transform == null);

                    var scrollRect = gui.ControlObject.GetComponentInParent<ScrollRect>();
                    var layoutEle = gui.ControlObject.GetOrAddComponent<LayoutElement>();
                    tglGroup = gui.ControlObject.transform.parent.GetOrAddComponent<ToggleGroup>();
                    tglGroup.allowSwitchOff = true;


                    var imgObj = gui.ControlObject.GetComponentInChildren<RawImage>();

                    var tgl = imgObj.GetOrAddComponent<Toggle>();

                    tgl.group = tglGroup;

                    layoutEle.minWidth = 100;
                    layoutEle.minHeight = 165;
                    layoutEle.preferredWidth = -1;
                    layoutEle.preferredHeight = -1;

                    //try
                    //{
                    //	GameObject.DestroyImmediate(scrollRect.content.GetComponent<LayoutGroup>());
                    //}
                    //catch { }

                    var gridPar = new GameObject("Grid Layout Obj");
                    gridPar.transform.parent = scrollRect.content;
                    gridPar.AddComponent<RectTransform>();
                    gridLayout = gridPar.AddComponent<GridLayoutGroup>();
                    gui.ControlObject.transform.SetParent(gridPar.transform);
                    imgObj.ScaleToParent2D();
                    gridPar.ScaleToParent2D(changeheight: false);

                    int space = 7;
                    gridLayout.constraintCount = 3;
                    gridLayout.spacing = new Vector2(space, space * .5f);
                    gridLayout.cellSize = new Vector2(scrollRect.content.GetComponent<RectTransform>().rect.width / (gridLayout.constraintCount + .5f), layoutEle.minHeight);
                    gridLayout.cellSize = new Vector2(gridLayout.cellSize.x, gridLayout.cellSize.x * 1.3f);
                    gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                    gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
                    gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
                    gridLayout.childAlignment = TextAnchor.MiddleCenter;

                    var txtObj = new GameObject();
                    txtObj.transform.parent = tgl.transform;
                    var txt = txtObj.AddComponent<TextMeshProUGUI>();
                    txtObj.ScaleToParent2D(pwidth: .9f, pheight: .95f);

                    yield return new WaitUntil(() => txt.fontMaterial != null);//wait for TMPro to instantiate

                    txt.fontMaterial.shaderKeywords =
                    txt.fontMaterial.shaderKeywords?.AddItem("OUTLINE_ON").ToArray();

                    //Logger
                    //.LogDebug($"Material keywords:\n[{string.Join(",\n", txt.fontMaterial.shaderKeywords)}]");

                    //	txt.material = txt.fontMaterial;
                    txt.alignment = TextAlignmentOptions.Bottom;
                    txt.color = Color.white;

                    txt.outlineColor = Color.black;
                    txt.outlineWidth = 0.2f;
                    txt.autoSizeTextContainer = false;
                    txt.enableAutoSizing = true;
                    txt.raycastTarget = false;
                    txt.enableWordWrapping = true;
                    txt.fontSizeMax = 30;
                    txt.fontSizeMin = 7;
                    txt.SetAllDirty();

                    gui.ControlObject.SetActive(false);

                    yield break;
                }

                inst.StartCoroutine(SetupCo());
            });
            #endregion

            #region Top
            //costume Name
            infoTxt = e.AddControl(new MyMakerText("", category, inst))
                .AddToCustomGUILayout(topUI: true, newVertLine: true, pWidth: 0.70f, viewpercent: cfg.viewportUISpace.Value);

            e.AddControl(new MakerDropdown(settingName: "", options: sortOptions, initialValue: 0, category: category, owner: inst))
                .AddToCustomGUILayout(topUI: true, newVertLine: false, pWidth: 0.30f)
                .OnGUIExists(gui =>
                {
                    gui.ControlObject.GetTextComponentInChildren()?.gameObject.SetActive(false);

                    gui.ValueChanged.Subscribe((val) =>
                    {
                        var sort =
                        new Func<KeyValuePair<string, BodyDoubleData>, object>
                        ((k) =>
                        {
                            switch(val / 2)
                            {
                            case 0:
                                return (object)bdCtrl.data.ToList().IndexOf(k);
                            case 1:
                                return (object)k.Value.translatedName.ToLower().Trim();
                            case 2:
                                return (object)k.Value.created;
                            case 3:
                                return (object)k.Value.updated;
                            default:
                                return (object)bdCtrl.data.ToList().IndexOf(k);
                            }
                        });

                        List<KeyValuePair<string, BodyDoubleData>> order;
                        if(val % 2 == 0)
                            order = bdCtrl.data.OrderBy(sort).ToList();
                        else
                            order = bdCtrl.data.OrderByDescending(sort).ToList();

                        order.Do(valu =>
                        {
                            var tgl = (Toggle)valu.Value.extras.FirstOrNull(obj => obj is Toggle);
                            tgl?.transform.parent.SetAsLastSibling();
                        });

                    });
                });

            e.AddControl(new MakerTextbox(
#if KOI_API
                settingName: "Search",
#elif HONEY_API
                settingName: "",
#endif
                defaultValue: "", category: category, owner: inst))
                .AddToCustomGUILayout(topUI: true, newVertLine: true)
                .OnGUIExists((gui) =>
                {
#if KKS
                    var input = (TMP_InputField)gui.ControlObject?.GetInputFieldComponentInChildren();
#else
                    var input = (InputField)gui.ControlObject?.GetInputFieldComponentInChildren();
#endif

                    input.textComponent.alignment =
#if KKS
                    TextAlignmentOptions.MidlineLeft;
#else
                    TextAnchor.MiddleLeft;
#endif
                    input.ObserveEveryValueChanged((k) => k.text)
                    .Subscribe((val) =>
                    {
                        //	var rgxOp = RegexOptions.IgnorePatternWhitespace | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;

                        //de-activate all
                        bdCtrl.data
                        .Do((k) =>
                        {
                            var tgl = (Toggle)k.Value.extras.Find((obj) => obj is Toggle);
                            tgl?.transform.parent.gameObject.SetActive(false);
                        });

                        //find search
                        bdCtrl.data
                        .Where((k) => (k.Value.translatedName + k.Value.name).Search(val.Replace(" ", ""))
                        || val.IsNullOrWhiteSpace())
                            .Do((k) =>
                            {
                                var tgl = (Toggle)k.Value.extras.FirstOrNull((obj) => obj is Toggle);
                                tgl?.transform.parent.gameObject.SetActive(true);
                            });

                    });


#if KKS
                    var placehold = ((TMP_Text)input.placeholder);
#else
                    var placehold = ((Text)input.placeholder);
#endif
                    placehold.text = "Search...";
                    placehold.alignment =
#if KKS
                    TextAlignmentOptions.MidlineLeft;
#else
                    TextAnchor.MiddleLeft;
#endif

                });

            e.AddControl(new MakerSeparator(category, inst))
                .AddToCustomGUILayout(topUI: true);

            #endregion

            #region Bottom

            void CreatePartToggles()
            {
                ToggleGroup loadTglGroup = null;
                var fields = typeof(PartFeilds).GetFields();

                foreach(var field in fields)
                    e.AddControl(new MakerToggle(category, field.Name, inst)
                        .AddToCustomGUILayout(topUI: false, newVertLine: field == fields[0])
                        .OnGUIExists(gui =>
                        {
                            gui.Value = (bool)field.GetValue(partFields);

                            gui.ValueChanged.Subscribe((val) =>
                            {
                                field.SetValue(partFields, val);
                            });
                            Logger.LogInfo($"value change on gui");

                            partFields.ObserveEveryValueChanged((p) => (bool)field.GetValue(p))
                            .Subscribe((val) => gui.Value = val);

                            Logger.LogInfo($"Created Part Toggle for {field.Name}");
                        }));


            }
            CreatePartToggles();

            e.AddControl(new MakerToggle(category, "Make BodyDouble Persistant", cfg.areBodyDoublesPersistant.Value, inst))
                    .AddToCustomGUILayout(newVertLine: true)
                    .OnGUIExists((gui) =>
                    {
                        if(isPersistantHndl != null)
                            cfg.areBodyDoublesPersistant.SettingChanged -= isPersistantHndl;
                        cfg.areBodyDoublesPersistant.SettingChanged += isPersistantHndl =
                        (s, a) =>
                        {
                            if(gui.Value != cfg.areBodyDoublesPersistant.Value)
                                gui.Value = cfg.areBodyDoublesPersistant.Value;
                        };
                        //isPersistantHndl(null, null);

                        gui.ValueChanged.Subscribe((on) =>
                        {
                            cfg.areBodyDoublesPersistant.Value = on;
                        });
                    })
                    .tooltipMsg(cfg.areBodyDoublesPersistant.Description.Description, Instance, () => cfg.enableTooltips.Value);


            e.AddControl(new MyMakerButton("Use Selected", category, inst))
                .AddToCustomGUILayout(newVertLine: true)
                .OnClick.AddListener(() =>
                {
                    if(!tglGroup.AnyTogglesOn()) return;

                    bdCtrl.LoadBodyDouble(currentCoord, partFields);
                    Illusion.Game.Utils.Sound.Play(SystemSE.ok_l);
                });

            e.AddControl(new MyMakerButton("Use Default", category, inst))
                .AddToCustomGUILayout(newVertLine: false)
                .OnClick.AddListener(() =>
                {
                    bdCtrl.LoadDefaultBodyDouble(partFields);
                    Illusion.Game.Utils.Sound.Play(SystemSE.ok_l);
                });

            e.AddControl(new MyMakerButton("Add Card[s]", category, inst))
                .AddToCustomGUILayout(newVertLine: true)
                .OnClick.AddListener(() =>
                {
                    ForeGrounder.SetCurrentForground();
                    GetNewPresetImages(null);
                    ForeGrounder.RevertForground();
                });


            e.AddControl(new MyMakerText("Danger Zone", category, inst))
                .AddToCustomGUILayout(newVertLine: true)
                .OnGUIExists(gui =>
                {
                    gui.TextColor = Color.red;
                });

            e.AddControl(new MyMakerButton("Remove Selected", category, inst))
                .AddToCustomGUILayout(newVertLine: true)
                .OnGUIExists((gui) =>
                {
                    gui.TextColor = Color.red;
                    gui.ButtonColor = Color.white.RGBMultipliedExt(0.35f);
                })
                .OnClick.AddListener(() =>
                {
                    if(!tglGroup.AnyTogglesOn()) return;

                    bdCtrl.RemoveBodyDouble(currentCoord);
                    Illusion.Game.Utils.Sound.Play(SystemSE.cancel);
                });

            e.AddControl(new MyMakerButton("Remove All", category, inst))
                .AddToCustomGUILayout(newVertLine: false)
                .OnGUIExists((gui) =>
                {
                    gui.TextColor = Color.red;
                    gui.ButtonColor = Color.white.RGBMultipliedExt(0.35f);
                })
                .OnClick.AddListener(() =>
                {
                    foreach(var fash in bdCtrl.data.Values.ToList())
                        bdCtrl.RemoveBodyDouble(fash);
                    Illusion.Game.Utils.Sound.Play(SystemSE.cancel);
                });
            #endregion
        }

        public static void AddBodyDouble(in BodyDoubleData data)
        {
            if(!InsideMaker) return;

            Instance.StartCoroutine(AddBodyDoubleCO(data));
        }

        public static void RemoveBodyDouble(in BodyDoubleData data)
        {
            if(!InsideMaker) return;

            Instance.StartCoroutine(RemoveBodyDoubleCO(data));
        }

        public static void RemoveAllBodyDoubles()
        {
            if(!InsideMaker) return;

            Instance.StartCoroutine(RemoveAllBodyDoublesCO());
        }

        #region Coroutine Helpers   

        static IEnumerator AddBodyDoubleCO(BodyDoubleData body)
        {
            if(body == null) yield break;

            yield return new WaitWhile(() => gridLayout == null);

            //	for( int a=0;a<12;++a)
            //		yield return null;



            var comp = Instantiate(template.ControlObject, template.ControlObject.transform.parent);
            var img = comp.GetComponentInChildren<RawImage>();
            img.texture = body.cardData?.LoadTexture(TextureFormat.RGBA32);

            var tgl = comp.GetComponentInChildren<Toggle>();
            var txt = tgl.GetComponentInChildren<TMP_Text>();
            body.extras.Add(tgl);

            txt.text = body.translatedName;
            comp.GetComponent<MakerImage>().tooltipMsg(body.translatedName, Instance);

            tgl.targetGraphic = img;
            var colours = tgl.colors = new ColorBlock()
            {
                normalColor = Color.white,
                highlightedColor = new Color(1, 1, 1, .75f),
                pressedColor = new Color(1, 1, 1, .45f),
                colorMultiplier = 1,
                fadeDuration = 0.1f,
            };

            tgl.onValueChanged.AddListener((val) =>
            {
#if KKS
                //	colours.selectedColor = Color.white;
                img.color = Color.white;
#else
                img.color = Color.white;
#endif
                currentCoord = null;

                if(!val) return;

#if KKS
                //		colours.selectedColor = Color.green - new Color(0, 0, 0, .15f);
                img.color = Color.green - new Color(0, 0, 0, .15f);
#else
                img.color = Color.green - new Color(0, 0, 0, .15f);
#endif

                currentCoord = body;

#if true //!KKS
                tgl.InstantClearState();
#endif
            });

            bool last = false;
            tgl.ObserveEveryValueChanged(p => p.isPointerInside).Subscribe((hover) =>
            {
                if(last == hover) return;//not sure if this does anything...

                if(hover)
                {
                    infoTxt.Text = body.name;
                    infoTxt.TextColor = Color.yellow;
                }
                else
                {
                    infoTxt.Text = GetAllChaFuncCtrlOfType<BodyDouble_Controller>()
                    .FirstOrNull()?.data.Values
                    .FirstOrNull(j => (Toggle)j.extras.FirstOrNull(k => k is Toggle) == tgl.group.ActiveToggles().FirstOrNull())
                    ?.name ?? (tgl.group.AnyTogglesOn() ? infoTxt.Text : "");//May find something better in the future (I hope so 😰)

                    if(tgl.group.AnyTogglesOn())
                        infoTxt.TextColor = Color.green;
                    else
                        infoTxt.TextColor = Color.yellow;
                }

                last = hover;
            });

            comp.SetActive(true);
            yield break;
        }

        static IEnumerator RemoveBodyDoubleCO(BodyDoubleData coordinate)
        {
            if(coordinate == null) yield break;

            yield return new WaitWhile(() => gridLayout == null);

            Toggle tmp = (Toggle)coordinate.extras.FirstOrNull((obj) => obj is Toggle);
            if(tmp)
                Destroy(tmp.transform.parent.gameObject);

            if(tmp.isOn) currentCoord = null;

            yield break;
        }

        static IEnumerator RemoveAllBodyDoublesCO()
        {

            yield return new WaitWhile(() => gridLayout == null);

            foreach(var tmp in tglGroup.ActiveToggles())
                Destroy(tmp.transform.parent.gameObject);

            currentCoord = null;

            yield break;
        }

        #endregion

        #region File Stuff

        #region File Data
        public const string FileExt = ".png";
        public const string FileFilter = "Coordinate Images (*.png)|*.png";

        public static string DefaultCardDirectory { get => (Directory.GetCurrentDirectory() + "/UserData/chara/").MakeDirPath("/", "\\"); }

        public static string TargetDirectory { get => Directory.Exists(cfg.lastCoordDir.Value) && !cfg.lastCoordDir.Value.IsNullOrWhiteSpace() ? cfg.lastCoordDir.Value : DefaultCardDirectory; }

        #endregion

        //private static string MakeDirPath(string path) => MakeDirPath(path);

        public static void GetCoordinatesInFolder(BodyDouble_Controller ctrl = null)
        {

            //var paths = OpenFileDialog.ShowDialog("Add all files in this folder (you may have to choose one)",
            //Directory.Exists(cfg.lastCoordDir.Value) ?
            // cfg.lastCoordDir.Value : TargetDirectory,
            //"Folder",
            //"",
            //OpenFileDialog.SingleFileFlags,
            //owner: ForeGrounder.Handle);

            //var path = paths?.Attempt((s) => s.IsNullOrWhiteSpace() ?
            //throw new Exception() : s).LastOrNull().MakeDirPath();

            SystemFileDialog.ShowDialog("Add all files in this folder (you may have to choose one)",
               ( Directory.Exists(cfg.lastCoordDir.Value) ?
                cfg.lastCoordDir.Value : TargetDirectory).MakeDirPath("/", "\\"), 
                out var paths,
                FOS.PICKFOLDERS | FOS.DONTADDTORECENT | FOS.NODEREFERENCELINKS |
                FOS.OKBUTTONNEEDSINTERACTION | FOS.STRICTFILETYPES | FOS.PATHMUSTEXIST
            );
            var pathsArray = paths.Split(';', ',');

            var path = pathsArray?.Attempt((s) => s.IsNullOrWhiteSpace() ?
            throw new Exception() : s).LastOrNull().MakeDirPath();

            cfg.lastCoordDir.Value = path?.Substring(0, path.LastIndexOf('/')) ?? TargetDirectory;

            OnCoordinateFolderObtained(cfg.lastCoordDir.Value, ctrl);
            if(path.IsNullOrWhiteSpace())
                Illusion.Game.Utils.Sound.Play(SystemSE.ok_l);
            else
                Illusion.Game.Utils.Sound.Play(SystemSE.cancel);

        }

        public static void GetNewPresetImages(BodyDouble_Controller ctrl = null)
        {
            //	OpenFileDialog.OpenSaveFileDialgueFlags.OFN_CREATEPROMPT;
            Logger.LogInfo("Game Root Path: " + Directory.GetCurrentDirectory());

            SystemFileDialog.ShowDialog(
                "Add New Character Card[s] (You can select multiple)",
               (Directory.Exists(cfg.lastCoordDir.Value) ?
                cfg.lastCoordDir.Value : TargetDirectory).MakeDirPath("/", "\\"),
                out var paths,
                filter: FileFilter,
                fos: FOS.ALLOWMULTISELECT | FOS.DONTADDTORECENT | FOS.FILEMUSTEXIST |
                FOS.NODEREFERENCELINKS | FOS.OKBUTTONNEEDSINTERACTION | FOS.STRICTFILETYPES
            );

            if(paths == null) return;

            //var path = paths?.Attempt((s) => s.IsNullOrWhiteSpace() ?
            //throw new Exception() : s).LastOrNull().MakeDirPath();
            var pathsAray = paths.MakeDirPath().Split('|');//they split by this character

            Logger.LogInfo("Paths obtained: " + paths);
            cfg.lastCoordDir.Value = pathsAray.LastOrNull()?.Substring(0, pathsAray.LastOrNull().LastIndexOf('/')) ?? TargetDirectory;

            OnCoordinateImagesObtained(ref pathsAray, ctrl);
            if(paths.IsNullOrWhiteSpace())
                Illusion.Game.Utils.Sound.Play(SystemSE.ok_l);
            else
                Illusion.Game.Utils.Sound.Play(SystemSE.cancel);
        }

        /// <summary>
        /// Called after a file is chosen in file explorer menu  
        /// </summary>
        /// <param name="strings: ">the info returned from file explorer. strings[0] returns the full file path</param>
        private static void OnCoordinateImagesObtained(ref string[] strings, BodyDouble_Controller ctrl = null)
        {

            ForeGrounder.RevertForground();
            if(cfg.debug.Value) Logger.LogDebug($"Enters accept");
            if(strings == null || strings.Length == 0) return;

            foreach(string s in strings)
            {
                var texPath = s.MakeDirPath();

                if(cfg.debug.Value)
                {
                    Logger.LogDebug($"Original path: {texPath}");
                    Logger.LogDebug($"texture path: {Path.Combine(Path.GetDirectoryName(texPath), Path.GetFileName(texPath))}");
                }

                if(texPath.IsNullOrWhiteSpace())
                    continue;


                //	var directory = Path.GetDirectoryName(texPath).MakeDirPath();
                var filename = texPath.Substring(texPath.LastIndexOf('/') + 1).MakeDirPath();//not sure why this happens on hs2?

                //use file

                var fashCtrls = InsideStudio ?
                    GetSelectedControllers<BodyDouble_Controller>() :
                    ctrl != null ? new List<BodyDouble_Controller> { ctrl } :
#if KKS
                    Character.GetCharaList(1).Concat(Character.GetCharaList(0)).Select(mctrl => mctrl.GetComponent<BodyDouble_Controller>());
#else
                    Character.Instance.GetCharaList(1).Concat(Character.Instance.GetCharaList(0)).Select(mctrl => mctrl.GetComponent<BodyDouble_Controller>());
#endif




                var chaFileCtrl = new ChaFileControl();
                chaFileCtrl.LoadCharaFile(texPath);

                var name = filename.Substring(0, filename.LastIndexOf('.'));
                name = !chaFileCtrl.parameter.fullname.IsNullOrWhiteSpace() ?
                    chaFileCtrl.parameter.fullname ?? name : name;

                var data = new BodyDoubleData()
                {
                    cardData = File.ReadAllBytes(texPath),
                    name = name,
                    created = File.GetCreationTime(texPath),
                    updated = File.GetLastWriteTime(texPath)
                };


                foreach(var mctrl in fashCtrls)
                    mctrl.AddBodyDouble(data);

            }

            if(cfg.debug.Value) Logger.LogDebug($"Exit accept");
        }

        static void OnCoordinateFolderObtained(string folder, BodyDouble_Controller ctrl = null)
        {
            var files = Directory.GetFiles(folder);
            OnCoordinateImagesObtained(ref files, ctrl);
        }

        public static void GetNewBGUIPath()
        {
            //	OpenFileDialog.OpenSaveFileDialgueFlags.OFN_CREATEPROMPT;
            Logger.LogInfo("Game Root Path: " + Directory.GetCurrentDirectory());

            SystemFileDialog.ShowDialog(
               "Add New Character Card[s] (You can select multiple)",
               Directory.Exists(cfg.lastCoordDir.Value) ?
               cfg.lastCoordDir.Value : TargetDirectory,
               out var paths,
               filter: FileFilter + ";*.jpg;*.jpeg;*.webp",
               fos: FOS.DONTADDTORECENT | FOS.FILEMUSTEXIST |
               FOS.NODEREFERENCELINKS | FOS.OKBUTTONNEEDSINTERACTION | FOS.STRICTFILETYPES
           );

            //var paths = OpenFileDialog.ShowDialog("Select Background",
            //TargetDirectory.MakeDirPath("/", "\\"),
            //FileFilter,
            //FileExt,
            //OpenFileDialog.SingleFileFlags,
            //owner: ForeGrounder.Handle);

            //var path = paths?.Attempt((s) => s.IsNullOrWhiteSpace() ?
            //throw new Exception() : s).LastOrNull().MakeDirPath();

            ForeGrounder.RevertForground();
            OnBGImageObtained(paths);
        }

        private static void OnBGImageObtained(string path)
        {
            if(path.IsNullOrWhiteSpace()) return;

            cfg.bgUIImagePath.Value = path;
        }
        #endregion


    }
}
