using System;
using System.Collections.Generic;
using System.Text;

namespace PhantasmaApp.Helpers
{
    public static class Utility
    {

        public static void Breakpoint(bool condition = true)
        {
            if (System.Diagnostics.Debugger.IsAttached && condition)
            {
                System.Diagnostics.Debugger.Break();
            }
        }

    }
}
