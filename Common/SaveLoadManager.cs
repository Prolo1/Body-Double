using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ExtensibleSaveFormat;

using MessagePack;
using MessagePack.Resolvers;

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

        protected override PluginData UpdateVersionFromPrev(BodyDouble_Controller ctrl, PluginData data)
        {
            if(data == null)
                data = ctrl?.GetExtendedData(true);

            return data;
        }

        public override PluginData Load(BodyDouble_Controller ctrl, PluginData data = null)
        {
            data = UpdateVersionFromPrev(ctrl, data);
            if(data == null) return null;

            //ADD CODE HERE
            try
            {

                if(data.version != Version) throw new Exception($"Target data was incorrect version: expected [V{Version}] instead of [V{data.version}]");

                var cardData = LZ4MessagePackSerializer.Deserialize<Dictionary<string, BodyDoubleData>>
                    ((byte[])data.data[DataKeys[(int)LoadDataType.Data]], FormatterResolver);

                if(cardData == null) throw new Exception("Deserialized card data was null");

                foreach(var kvp in cardData)
                    ctrl.AddBodyDouble(kvp.Value);

            }
            catch(Exception e)
            {
                Logger.Log(Error | Message, $"Could not load PluginData:\n{e.Message}");
                Logger.Log(Error, $"\n{e.TargetSite}\n{e.StackTrace}\n");
                return null;
            }


            return data;
        }

        public override PluginData Save(BodyDouble_Controller ctrl, PluginData data = null)
        {
            data = data ?? new PluginData();
            data.version = Version;

            //ADD CODE HERE
            try
            {
                var dataLine = ctrl.data.ToDictionary((k) => k.Key, (v) => v.Value.Clone());
                foreach(var bit in dataLine)
                    bit.Value?.extras?.Clear(); //don't save extras, since they are not needed and can cause issues with serialization

                data.data[DataKeys[(int)LoadDataType.Data]] = LZ4MessagePackSerializer.Serialize(dataLine, FormatterResolver);

                ctrl.SetExtendedData(data);
            }
            catch(Exception e)
            {
                Logger.Log(Error | Message, $"Could not save PluginData:\n{e.Message}");
                Logger.Log(Error, $"\n{e.TargetSite}\n{e.StackTrace}\n");
                data = null;
            }
            finally
            {
                if(data != null)
                    Logger.Log(Info | Message, $"Sucessfully saved Body Dobble data to card");
            }

            return data;

        }
    }
}
