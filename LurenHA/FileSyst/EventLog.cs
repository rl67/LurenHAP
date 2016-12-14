using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSyst
{
    /// <summary>
    /// Event log, append messages
    /// </summary>
    static class EventLog
    {
        static public void WriteEvent(string lhaEventStr)
        {
            string m_eventFile = @"\\ALM\backup\Data\Programmering\VisualStudio\LurenHA\LurenHA\LurenHA\bin\Debug\logs\lhaEvents.log";

            using (System.IO.StreamWriter file = new System.IO.StreamWriter(m_eventFile, true))
            {
                file.WriteLine(DateTime.UtcNow.ToString("yyyy-MM-dd-HH:mm:ss.fff") + "\t" + lhaEventStr);
            }

        }

        static public string[] GetEvets()
        {
            return System.IO.File.ReadAllLines(@"\\ALM\backup\Src\VS\LurenHA\LurenHA\LurenHA\bin\Debug\logs/lhaEvents.log");
        }
    }
}
