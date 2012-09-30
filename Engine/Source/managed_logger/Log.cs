using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace managed_logger
{
    class Log
    {
        public string Timestamp { get; set; }
        public string Type { get; set; }
        public string Channel { get; set; }
        public string Source { get; set; }
        public string Condition { get; set; }
        public string Message { get; set; }
        public string ClassName { get; set; }
        public string StateName { get; set; }
        public string FuncName { get; set; }
    }
}
