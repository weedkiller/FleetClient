﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetDaemon.Storage.Interfaces
{
    public interface ISimpleStorage
    {
        T Get<T>(string key);
        bool Store(Dictionary<string, object> dict);
        bool Store(string key, object value);
    }
}
