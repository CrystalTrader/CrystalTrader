﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CrystalTrader.Web.Models
{
    public class BaseViewModel
    {
        public string InstanceName { get; set; }
        public string Version { get; set; }
        public bool ReadOnlyMode { get; set; }
    }
}
