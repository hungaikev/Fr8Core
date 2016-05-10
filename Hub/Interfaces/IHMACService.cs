﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Hub.Interfaces
{
    public interface IHMACService
    {
        Task<string> CalculateHMACHash(Uri requestUri, string userId, string terminalId, string terminalSecret, string timeStamp, string nonce, HttpContent content);
        Task<Dictionary<string, string>> GenerateHMACHeader(Uri requestUri, string terminalId, string terminalSecret, string userId, HttpContent content);
        Task<Dictionary<string, string>> GenerateHMACHeader<T>(Uri requestUri, string terminalId, string terminalSecret, string userId, T content);
        Task<Dictionary<string, string>> GenerateHMACHeader(Uri requestUri, string terminalId, string terminalSecret, string userId);
    }
}