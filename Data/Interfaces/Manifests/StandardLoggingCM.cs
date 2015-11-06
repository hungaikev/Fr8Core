﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Data.Interfaces.DataTransferObjects;

namespace Data.Interfaces.Manifests
{
    public class StandardLoggingCM : Manifest
    {
        public List<LogItemDTO> Item { get; set; }

        public StandardLoggingCM()
            : base(Constants.MT.StandardLoggingCrate)
        {
        }
    }
}
