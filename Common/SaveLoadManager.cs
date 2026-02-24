using System;
using System.Collections.Generic;
using System.Text;

using ExtensibleSaveFormat;

using MessagePack;

using ProloAPI;


namespace BodyDouble
{
    using static BodyDouble_Core;
    using static BodyDouble_Controller;
    using static BepInEx.Logging.LogLevel;

    public class CurrentSaveLoadManager : SaveLoadManager<BodyDouble_Controller, PluginData>
    {


        public new int Version => base.Version + 1;
        public new string[] DataKeys => new string[] { "BodyData_Data" };
        public new enum LoadDataType
        {
            Data
        }

        protected override PluginData UpdateVersionFromPrev(BodyDouble_Controller ctrler, PluginData data)
        {
            if(data == null)
                data = ctrler?.GetExtendedData(true);

            return data;
        }

        public override PluginData Load(BodyDouble_Controller ctrler, PluginData data = null)
        {
            data = UpdateVersionFromPrev(ctrler, data);
            if(data == null) return null;

            //ADD CODE HERE
            try
            {

                if(data.version != Version) throw new Exception($"Target data was incorrect version: expected [V{Version}] instead of [V{data.version}]");

                var cardData = LZ4MessagePackSerializer.Deserialize<Dictionary<string, BodyDoubleData>>
                    ((byte[])data.data[DataKeys[(int)LoadDataType.Data]]);

                foreach(var kvp in cardData)
                    ctrler.AddBodyDouble(kvp.Value);

            }
            catch(Exception e)
            {
                Logger.Log(Error | Message, $"Could not load PluginData:\n{e.Message}");
                Logger.Log(Error, $"\n{e.TargetSite}\n{e.StackTrace}\n");
                return null;
            }
            return data;
        }

        public override PluginData Save(BodyDouble_Controller ctrler, PluginData data = null)
        {
            data = data ?? new PluginData();
            data.version = Version;

            //ADD CODE HERE
            try
            {
                foreach(var bit in ctrler.data)
                    bit.Value?.extras?.Clear(); //don't save extras, since they are not needed and can cause issues with serialization

                data.data[DataKeys[(int)LoadDataType.Data]] = LZ4MessagePackSerializer.Serialize(ctrler.data);
            }
            catch(Exception e)
            {
                Logger.Log(Error | Message, $"Could not save PluginData:\n{e.Message}");
                Logger.Log(Error, $"\n{e.TargetSite}\n{e.StackTrace}\n");
                return null;
            }

            return data;

        }
    }
}
