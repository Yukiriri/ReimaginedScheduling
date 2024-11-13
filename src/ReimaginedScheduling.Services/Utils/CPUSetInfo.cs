using Vanara.PInvoke;

namespace ReimaginedScheduling.Services.Utils
{
    public static class CPUSetInfo
    {
        static CPUSetInfo()
        {
            foreach (var cset in CoreSetList)
            {
                PCoreIndex = Math.Max(PCoreIndex, cset.CpuSet.EfficiencyClass);
            }
            foreach (var cset in CoreSetList)
            {
                var cs = cset.CpuSet;
                if (cs.EfficiencyClass == PCoreIndex)
                {
                    PCoreList.Add(cs.Id);
                    if (cs.CoreIndex == cs.LogicalProcessorIndex)
                    {
                        PhysicalPCoreList.Add(cs.Id);
                        PhysicalPECoreList.Add(cs.Id);
                    }
                }
                else
                {
                    ECoreList.Add(cs.Id);
                    PhysicalPECoreList.Add(cs.Id);
                }
                PECoreList.Add(cs.Id);
            }
        }

        public static void WriteLine()
        {
            string[] description = ["未知", "未知", "未知", "未知"];
            if (PCoreIndex >= 2) description[PCoreIndex - 2] = "LPE核";
            if (PCoreIndex >= 1) description[PCoreIndex - 1] = "E核";
            if (PCoreIndex >= 0) description[PCoreIndex - 0] = "P核";
            foreach (var cset in CoreSetList)
            {
                var cs = cset.CpuSet;
                Console.WriteLine($"ID:{cs.Id,-3} 物理核心:{cs.CoreIndex,-3} 逻辑核心:{cs.LogicalProcessorIndex,-3} 异构类型:{description[cs.EfficiencyClass],-5}");
            }
            Console.WriteLine($"CPU异构组合：{PhysicalPCoreList.Count}P + {ECoreList.Count}E");
            Console.WriteLine($"P核：[{PCoreList.First()} - {PCoreList.Last()}]");
            Console.WriteLine($"物理P核：{string.Join(" ", PhysicalPCoreList)}");
            if (!IsPCoreOnly)
            {
                Console.WriteLine($"E核：[{ECoreList.First()} - {ECoreList.Last()}]");
            }
        }

        private static Kernel32.SYSTEM_CPU_SET_INFORMATION[] CoreSetList { get; } = Kernel32.GetSystemCpuSetInformation().ToArray();
        public static int PCoreIndex { get; private set; } = 0;
        public static bool IsPCoreOnly
        {
            get
            {
                return PCoreIndex == 0;
            }
        }
        public static List<uint> PCoreList { get; private set; } = [];
        public static List<uint> PhysicalPCoreList { get; private set; } = [];
        public static List<uint> ECoreList { get; private set; } = [];
        public static List<uint> PECoreList { get; private set; } = [];
        public static List<uint> PhysicalPECoreList { get; private set; } = [];
    }
}
