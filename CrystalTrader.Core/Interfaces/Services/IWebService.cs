using System;
using System.Collections.Generic;
using System.Text;

namespace CrystalTrader.Core
{
    public interface IWebService
    {
        IWebConfig Config { get; }
        void Start();
        void Stop();
    }
}
