using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;


using KKAPI;
using KKAPI.Chara;
using KKAPI.Maker;
using KKAPI.Utilities;

using ExtensibleSaveFormat;

using Manager;
#if HONEY_API
using AIChara;

#endif

using ProloAPI.Extensions;

using Ctrler = BodyDouble.BodyDouble_Controller;

namespace BodyDouble
{
    using static BodyDouble_Core;
    public class BodyDouble_Controller : CharaCustomFunctionController
    {
        public static string LastCardSaveLocation { get; private set; }
            = BodyDouble_GUI.DefaultCardDirectory;
        public Dictionary<string, BodyDoubleData> data = new Dictionary<string, BodyDoubleData>();

        PluginData extData = null;
        BodyDoubleData defaultData = null;

        bool isLoadingDouble = false;
        void OnCharaReload(GameMode currentGameMode, bool keepState = false)
        {
            if(isLoadingDouble) return;

            //Logger.LogInfo($"File Name: [{ChaFileControl.ConvertCharaFilePath(ChaFileControl.charaFileName, ChaControl.sex)}]");
            Logger.LogInfo($"Saving default character: [{ChaFileControl.parameter.fullname}]");

            using(MemoryStream st = new MemoryStream())
            {

                ChaFileControl.SaveCharaFile(st, false);
                byte[] buffer = st?.GetBuffer()?.ToArray();//have to copy the array otherwise stream close will invalidate it

                //Logger.LogWarning($"checking data saved [{ChaFileControl.parameter.fullname}], bytes[{buffer?.Length}] ");

                defaultData = new BodyDoubleData()
                {
                    cardData = buffer,
                    name = ChaFileControl.parameter.fullname,
                    created = DateTime.Now,
                    updated = DateTime.Now
                };
            }

            if(!cfg.areBodyDoublesPersistant.Value)
                data.Clear();

            extData = this.LoadExtData<CurrentSaveLoadManager, Ctrler>();

        }

        public void AddBodyDouble(BodyDoubleData data)
        {
            if(data == null) return;

            if(this.data.ContainsKey(data.name))
                BodyDouble_GUI.RemoveBodyDouble(this.data[data.name]);

            BodyDouble_GUI.AddBodyDouble(data);

            this.data[data.name] = data;
        }

        public void RemoveBodyDouble(BodyDoubleData data)
        {
            if(data == null) return;
            this.data.Remove(data.name);

            BodyDouble_GUI.RemoveBodyDouble(data);
        }

        public void LoadBodyDouble(BodyDoubleData data, PartFeilds parts)
        {
            isLoadingDouble = true;
            ChaFileControl.LoadFileLimited(CreateTmpCardFile(data.cardData), ChaControl.sex, parts.face, parts.body, parts.hair, parts.perameter, parts.coordinate);
            BodyReload(parts);
            isLoadingDouble = false;
        }

        public void LoadDefaultBodyDouble(PartFeilds parts) => LoadBodyDouble(defaultData, parts);

        private void BodyReload(PartFeilds parts, bool reload = true)
        {

            try
            {
                if(cfg.debug.Value)
                    Logger.LogDebug("Fashion reload called");

#if HONEY_API
                Singleton<Character>.Instance.customLoadGCClear = false;
#endif

//                ChaControl.AssignCoordinate(
//#if KOI_API
//                    (ChaFileDefine.CoordinateType)ChaControl.chaFile.status.coordinateType, 
//#endif
//                    ChaControl.nowCoordinate
//                );

                if(reload)
                    ChaControl.Reload(!parts.coordinate, !parts.face, !parts.hair, !parts.body
#if HONEY_API
                            , true
#endif
                    );
#if HONEY_API
                Singleton<Character>.Instance.customLoadGCClear = true;
#endif




                if(MakerAPI.InsideMaker)
                {

                    var mkBase = MakerAPI.GetMakerBase();

#if HONEY_API
                    mkBase.ChangeAcsSlotName(-1);
                    mkBase.forceUpdateAcsList = true;
#elif KOI_API
                    mkBase.updateCvsAccessoryChange =
                    mkBase.updateCvsAccessoryCopy = true;
#endif
                    mkBase.updateCustomUI = true;
                }
            }
            catch(Exception e) { Logger.LogError($"Reload did not complete:\n{e}"); }
        }


        private string CreateTmpCardFile(byte[] data)
        {
            string tmpLocation = $"{(Directory.GetCurrentDirectory() + "/userdata/Tmp/BDouble.png").MakeDirPath("/", "\\")}";

            File.WriteAllBytes(tmpLocation, data);

            var lastSave = LastCardSaveLocation;

            if(File.Exists(lastSave))
            {
                if(File.Exists(tmpLocation))
                    File.Delete(tmpLocation);
                File.Move(lastSave, tmpLocation);
            }

            if(!File.Exists(tmpLocation))
                throw new FileNotFoundException($"Could not create tmp file: {tmpLocation}");

            return tmpLocation;
        }

        private string CreateTmpCardFile(ChaFile data)
        {
            string tmpLocation = $"{(Directory.GetCurrentDirectory() + "/userdata/Tmp/BDouble.png").MakeDirPath("/", "\\")}";

            //saveCoordExtDataTo(data, data);
            //data.pngData = UIGoku.EncodeToPNG();
            data?.SaveFile(Path.GetFileName(tmpLocation)
#if HONEY_API
                , (int)Singleton<GameSystem>.Instance.language
#endif
                );


            var lastSave = LastCardSaveLocation;

            Directory.CreateDirectory(Path.GetDirectoryName(tmpLocation));

            if(File.Exists(lastSave))
            {
                if(File.Exists(tmpLocation))
                    File.Delete(tmpLocation);
                File.Move(lastSave, tmpLocation);
            }

            if(!File.Exists(tmpLocation))
                throw new FileNotFoundException($"Could not create tmp file: {tmpLocation}");


            return tmpLocation;
        }


        #region Classes

        public class BodyDoubleData
        {
            public byte[] cardData;
            public string name;
            public DateTime updated;
            public DateTime created;
            public string translatedName
            {
                get
                {
                    TranslationHelper.TryTranslate(name, out var trans);
                    return trans ?? name;
                }
            }

            public List<object> extras = new List<object>();
        }

        public class PartFeilds
        {
            public bool
                face = true,
                body = true,
                hair = true,
                coordinate = false,
                perameter = false;
        }
        #endregion

        #region Overrides
        protected override void OnReload(GameMode currentGameMode, bool maintainState)
        {

            OnCharaReload(currentGameMode, maintainState);
        }

        protected override void OnCardBeingSaved(GameMode currentGameMode)
        {
            this.SaveExtData<CurrentSaveLoadManager, Ctrler>();
        }


        #endregion
    }
}
