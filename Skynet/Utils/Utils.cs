﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Skynet.Utils
{
    class Utils
    {
        public static bool isValidGuid(string guid) {
            Guid mGuid;
            return Guid.TryParse(guid, out mGuid);
        }
    }
}
