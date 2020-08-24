using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using OldRod.Core.Architecture;

namespace OldRod.Json
{
    public class ConstantsConfiguration
    {
        private static readonly JsonSerializer Serializer = new JsonSerializer();

        public static ConstantsConfiguration FromFile(string path)
        {
            using (var reader = new StreamReader(path))
                return FromReader(reader);
        }

        public static ConstantsConfiguration FromReader(TextReader reader)
        {
            return Serializer.Deserialize<ConstantsConfiguration>(new JsonTextReader(reader));
        }

        [JsonProperty("opcodes")]
        public Dictionary<string, byte> OpCodes
        {
            get;
        } = new Dictionary<string, byte>(StringComparer.OrdinalIgnoreCase);

        [JsonProperty("flags")]
        public Dictionary<string, byte> Flags
        {
            get;
        } = new Dictionary<string, byte>(StringComparer.OrdinalIgnoreCase);

        [JsonProperty("registers")]
        public Dictionary<string, byte> Registers
        {
            get;
        } = new Dictionary<string, byte>(StringComparer.OrdinalIgnoreCase);

        [JsonProperty("vmcalls")]
        public Dictionary<string, byte> VMCalls
        {
            get;
        } = new Dictionary<string, byte>(StringComparer.OrdinalIgnoreCase);

        [JsonProperty("ecalls")]
        public Dictionary<string, byte> ECalls
        {
            get;
        } = new Dictionary<string, byte>(StringComparer.OrdinalIgnoreCase);

        [JsonProperty("ehtypes")]
        public Dictionary<string, byte> EHTypes
        {
            get;
        } = new Dictionary<string, byte>(StringComparer.OrdinalIgnoreCase);

        [JsonProperty("misc")]
        public Dictionary<string, byte> Misc
        {
            get;
        } = new Dictionary<string, byte>(StringComparer.OrdinalIgnoreCase);

        private static IDictionary<string, T> CreateEnumDictionary<T>()
        {
            return Enum.GetValues(typeof(T))
                .Cast<T>()
                .Where(x => x.ToString() != "Max" && !x.ToString().StartsWith("__"))
                .ToDictionary(x => x.ToString(), x => x, StringComparer.OrdinalIgnoreCase);
        }

        private static void Process<T>(IDictionary<string, byte> config, IDictionary<string, T> stringToEnum, IDictionary<byte, T> target)
        {
            foreach (var entry in config)
            {
                if (!stringToEnum.TryGetValue(entry.Key, out var enumKey))
                    throw new KeyNotFoundException($"Unknown constant {entry.Key}.");
                
                if (target.TryGetValue(entry.Value, out var existing))
                    throw new ArgumentException(
                        $"Duplicate mapping found. {existing} and {enumKey} both map to {entry.Value} (0x{entry.Value:X2}).");

                stringToEnum.Remove(entry.Key);
                target.Add(entry.Value, enumKey);
            }
        }

        public VMConstants CreateVmConstants()
        {
            var ilCodes = CreateEnumDictionary<ILCode>();
            var flags = CreateEnumDictionary<VMFlags>();
            var registers = CreateEnumDictionary<VMRegisters>();
            var vmCalls = CreateEnumDictionary<VMCalls>();
            var eCalls = CreateEnumDictionary<VMECallOpCode>();
            var ehTypes = CreateEnumDictionary<EHType>();
            
            var constants = new VMConstants();
            var missing = new List<string>();

            Process(OpCodes, ilCodes, constants.OpCodes);
            Process(Flags, flags, constants.Flags);
            Process(Registers, registers, constants.Registers);
            Process(VMCalls, vmCalls, constants.VMCalls);
            Process(ECalls, eCalls, constants.ECallOpCodes);
            Process(EHTypes, ehTypes, constants.EHTypes);
            
            missing.AddRange(ilCodes.Keys);
            missing.AddRange(flags.Keys);
            missing.AddRange(registers.Keys);
            missing.AddRange(vmCalls.Keys);
            missing.AddRange(eCalls.Keys);
            missing.AddRange(ehTypes.Keys);

            if (Misc.TryGetValue("HELPER_INIT", out byte value))
                constants.HelperInit = value;
            else
                missing.Add("HELPER_INIT");
            
            if (Misc.TryGetValue("FLAG_INSTANCE", out value))
                constants.FlagInstance = value;
            else
                missing.Add("FLAG_INSTANCE");

            int missingCount = missing.Count;
            if (missingCount > 0)
            {
                string suffix = string.Empty;
                if (missingCount > 5)
                {
                    missing.RemoveRange(5, missingCount - 6);
                    suffix = $" and {missingCount - 6} more";
                }

                throw new ArgumentException(
                    $"Incomplete configuration file. Missing constants {string.Join(", ", missing)}{suffix}.");
            }
            
            return constants;
        }

    }
}
