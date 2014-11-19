﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sdl.Community.TMOptimizer
{
    public enum ProcessingState
    {
        NotProcessing,
        Processing,
        Canceling,
        Canceled,
        Completed,
        Failed
    }
}
