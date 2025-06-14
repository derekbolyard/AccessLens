using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccessLensApi.Tests.Scanner
{
    internal static class AxeShim
    {
        public const string Javascript =
            "window.axe={run:(doc,cb)=>cb(null,{violations:[]})};";
    }
}
